// Upgrade NOTE: upgraded instancing buffer 'MyProperties' to new syntax.

Shader "Custom/URP/ParticleShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100


        Pass
        {

            Tags { "LightMode" = "UniversalForward" }
            
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling
            // Enable shadow keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            // URP includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"


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
                float4 shadowCoord : TEXCOORD2;
            };

            UNITY_INSTANCING_BUFFER_START(PerInstanceData)
            UNITY_INSTANCING_BUFFER_END(PerInstanceData)

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float3x3 inverse(float3x3 m)
            {
                float a00 = m[0][0], a01 = m[0][1], a02 = m[0][2];
                float a10 = m[1][0], a11 = m[1][1], a12 = m[1][2];
                float a20 = m[2][0], a21 = m[2][1], a22 = m[2][2];

                float b01 = a22 * a11 - a12 * a21;
                float b11 = -a22 * a10 + a12 * a20;
                float b21 = a21 * a10 - a11 * a20;

                float det = a00 * b01 + a01 * b11 + a02 * b21;

                // Prevent division by zero
                if (abs(det) < 1e-6)
                    return float3x3(0, 0, 0, 0, 0, 0, 0, 0, 0); // Or handle error another way

                float invDet = 1.0 / det;

                float3x3 inverse;

                inverse[0][0] = (a11 * a22 - a12 * a21) * invDet;
                inverse[0][1] = -(a01 * a22 - a02 * a21) * invDet;
                inverse[0][2] = (a01 * a12 - a02 * a11) * invDet;

                inverse[1][0] = -(a10 * a22 - a12 * a20) * invDet;
                inverse[1][1] = (a00 * a22 - a02 * a20) * invDet;
                inverse[1][2] = -(a00 * a12 - a02 * a10) * invDet;

                inverse[2][0] = (a10 * a21 - a11 * a20) * invDet;
                inverse[2][1] = -(a00 * a21 - a01 * a20) * invDet;
                inverse[2][2] = (a00 * a11 - a01 * a10) * invDet;

                return inverse;
            }

            float3x3 PolarDecomposition(float3x3 F)
            {
                float3x3 R = F;
    
                for (int i = 0; i < 5; ++i)
                {
                    float3x3 R_T = transpose(R);
                    float3x3 R_invT = 0.5f * (3.0f * R - mul(R, mul(R_T, R)));
                    R = R_invT;
                }

                return inverse(R);
            }

            v2f vert (appdata v)
            {
                uint id = v.instanceID;
                Particle p = Particles[id];

                float3x3 rot = PolarDecomposition(p.F);
                float3 vert = mul(rot, v.vertex) + p.x;

                if (p.p1.x == 1) {
                    vert = v.vertex + p.x;
                    }
                    

                if (Removed[id] > 0) {
                    vert += 1e30f;
                }

                v2f o;
                o.vertex = TransformObjectToHClip(vert);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.col = p.color;

                float3 light = GetLight(int3(vert));

                float normal_light = (dot(normalize(float3(1, 1, 1)), normalize(mul(rot, v.normal))) + 3.0f) / 4.0f;

                light *= normal_light;

                o.col.xyz *= light;
                /*
                for (int x = -1; x <= 1; x++) {
                    for (int y = -1; y <= 1; y++) {
                        for (int z = -1; z <= 1; z++) {
                            light += GetLight(int3(vert + float3(x, y, z) * 1.0f));
                        }
                    }
                }
                o.col.xyz *= light / 28.0f;
                */

                float vel_test = clamp(length(p.v) * 0.25f, 0, 2);
                o.col.xyz = lerp(o.col.xyz * 0.5f, o.col.xyz, vel_test);

                float3 world = TransformObjectToWorld(vert);
                o.shadowCoord = TransformWorldToShadowCoord(world);
                
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                Light mainLight = GetMainLight();

                float shadow = MainLightRealtimeShadow(i.shadowCoord);

                // sample the texture
                float4 col = tex2D(_MainTex, i.uv) * i.col;
                if (col.a < 0.99f) {
                    discard;
                }

                col.xyz *= shadow;
                return col;
            }
            ENDHLSL
        }
    }
}
