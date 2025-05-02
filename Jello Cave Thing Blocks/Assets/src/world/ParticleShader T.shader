// Upgrade NOTE: upgraded instancing buffer 'MyProperties' to new syntax.

Shader "Unlit/ParticleShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

    }
    SubShader
    {
        Tags { 
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalRenderPipeline"    
        }
        LOD 100


        Pass
        {

            Tags{"LightMode"="UniversalForward"}

            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"


            struct Particle
            {
                float3 x;
                float mass;
                float3 v;
                float volume_0;
                float3x3 C;
                float3 p1;
                float3x3 F;
                float3 p2;
                float4 color;
            };

            StructuredBuffer<Particle> Particles;
            StructuredBuffer<uint> Removed;
            StructuredBuffer<float4> FloodFill;

            // Declare the textures
            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            int total_size;
            int total_height;

            float3 GetLight(int3 pos)
            {
                if (pos.x < 0 || pos.y < 0 || pos.z < 0 || pos.x >= total_size || pos.y >= total_height || pos.z >= total_size)
                {
                    return 1;
                }
                uint idx = uint(pos.x + pos.y * total_size + pos.z * total_size * total_height);
                
                float4 f = FloodFill[idx];

                return max(f.xyz, f.w);
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 col : COLOR;
                float4 screenPos : TEXCOORD1; // screen-space UV
                float3 normal : NORMAL;
            };

            UNITY_INSTANCING_BUFFER_START(PerInstanceData)
            UNITY_INSTANCING_BUFFER_END(PerInstanceData)

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                uint id = v.instanceID;
                Particle p = Particles[id];

                float3 vert = v.vertex + p.x;

                if (Removed[id] > 0) {
                    vert += 1e30f;
                }

                v2f o;
                o.vertex = TransformObjectToHClip(vert);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.col = p.color;
                o.normal = v.normal;

                o.screenPos = ComputeScreenPos(o.vertex);

                float3 light = GetLight(int3(vert));
                o.col.xyz *= light;


                float vel_test = clamp(length(p.v) * 0.25f, 0, 2);
                o.col.xyz = lerp(o.col.xyz * 0.5f, o.col.xyz, vel_test);
                
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 screenUV = i.screenPos.xy / i.screenPos.w; // Normalize by w

                float fragmentDepth = i.screenPos.z / i.screenPos.w;

                // sample the texture
                float4 col = tex2D(_MainTex, i.uv) * i.col;
                if (col.a >= 0.99f) {
                    discard;
                }

                half4 color = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV);
                float sceneDepth = SampleSceneDepth(screenUV);

                col = lerp(col, color, 1.0f - col.a);
                float n_light = (dot(i.normal, normalize(float3(1, 1, 1))) + 6.0f) / 7.0f;



                col = lerp(col, col * float4(n_light, n_light, n_light, 1.0f), clamp(1.0f - fragmentDepth * 5.0f, 0.0f, 0.5f));
                
                // apply fog
                return col;
            }
            ENDHLSL
        }
    }
}
