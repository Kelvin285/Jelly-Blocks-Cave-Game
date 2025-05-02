using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.src.world.entities
{
    public class Entity
    {
        public float width = 0.8f;
        public float height = 1.75f;
        public float3 position = new();
        public float3 motion = new();

        public float pitch, yaw;

        public bool on_ground = false;
        public bool in_water = false;

        public bool no_physics = false;
        public bool no_gravity = false;

        public JelloWorld world;

        public Entity(JelloWorld world)
        {
            this.world = world;
        }

        public float GetEyeHeight()
        {
            return height * 0.9f;
        }

        public virtual void CastRays()
        {

        }
        
        public quaternion GetLook()
        {
            return math.mul(quaternion.AxisAngle(new(0, 1, 0), math.radians(yaw)), quaternion.AxisAngle(new(1, 0, 0), math.radians(pitch)));
        }
        public void DoPhysics(int index)
        {
            var result = world.results[index];


            if (result.water > 0)
            {
                if (!no_physics)
                {
                    motion *= 0.95f;
                }
                in_water = true;
            } else
            {
                in_water = false;
            }

            if (result.down > 0)
            {
                on_ground = true;

                if (!no_physics)
                {
                    motion.x -= motion.x * Time.fixedDeltaTime;
                    motion.z -= motion.z * Time.fixedDeltaTime;
                    float p_vel = math.abs((result.down - 1) / 10000.0f);

                    if (motion.y < p_vel)
                    {
                        motion.y = p_vel;
                    }
                }
            } else
            {
                on_ground = false;
                
                if (!no_physics && !no_gravity)
                {
                    if (in_water)
                    {
                        motion.y -= 5.0f * Time.fixedDeltaTime;
                    }
                    else
                    {
                        motion.y -= 20.0f * Time.fixedDeltaTime;
                    }
                }
            }

            if (!no_physics)
            {
                if (result.up > 0)
                {
                    float p_vel = -math.abs((result.up - 1) / 10000.0f);

                    if (motion.y > p_vel)
                    {
                        motion.y = p_vel;
                    }
                }

                if (result.left > 0)
                {
                    float p_vel = math.abs((result.left - 1) / 10000.0f);

                    if (motion.x < p_vel)
                    {
                        motion.x = p_vel;
                    }
                }

                if (result.right > 0)
                {
                    float p_vel = -math.abs((result.right - 1) / 10000.0f);

                    if (motion.x > p_vel)
                    {
                        motion.x = p_vel;
                    }
                }

                if (result.front > 0)
                {
                    float p_vel = math.abs((result.front - 1) / 10000.0f);

                    if (motion.z < p_vel)
                    {
                        motion.z = p_vel;
                    }
                }

                if (result.back > 0)
                {
                    float p_vel = -math.abs((result.back - 1) / 10000.0f);

                    if (motion.z > p_vel)
                    {
                        motion.z = p_vel;
                    }
                }
            }

            position += motion * Time.fixedDeltaTime;

            if (!no_physics)
            {
                float step_up = (result.step_up / 10000.0f);


                if (step_up > 0 && on_ground)
                {
                    position.y += step_up;
                }
            }

            position = math.clamp(position, 3, new float3(65, 128, 65));
            Debug.Log(position);
        }

        public virtual void FixedUpdate()
        {
            pitch = math.clamp(pitch, -90, 90);
        }

        protected void DrawCube(float3 pos, float3 scale, quaternion rotation, Material mat)
        {

            Graphics.DrawMesh(world.ParticleMesh, Matrix4x4.Translate(new(pos.x, pos.y, pos.z)) * Matrix4x4.Scale(new(scale.x, scale.y, scale.z)) * Matrix4x4.Rotate(new(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w)), mat, 0);
        }

        public virtual void Render()
        {

        }
    }
}
