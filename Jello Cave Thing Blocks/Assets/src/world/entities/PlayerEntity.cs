using Assets.src.world.blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.src.world.entities
{
    public class PlayerEntity : Entity
    {
        private bool locked = false;
        private bool lastLocked = false;


        public PlayerEntity(JelloWorld world) : base(world)
        {
        }


        public int test_cast;
        public override void CastRays()
        {
            base.CastRays();

            quaternion look = GetLook();

            var look_vec = math.mul(look, new float3(0, 0, 1));

            test_cast = world.AddRaycast(position + new float3(0, GetEyeHeight(), 0), look_vec);
        }

        public bool left_click = false;
        public bool right_click = false;

        public float3 cast_hit_pos = new();
        public float3 cast_n_pos = new();

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            var cast = world.GetRaycast(test_cast);

            quaternion look = GetLook();

            var look_vec = math.mul(look, new float3(0, 0, 1));

            var eye_pos = position + new float3(0, GetEyeHeight(), 0);


            cast_hit_pos = eye_pos + look_vec * cast.dist;


            if (cast.index != -1)
            {
                var particle = cast.particle;

                var p_rot = math.inverse(world.PolarDecomposition(particle.F));

                var local_pos = math.mul(p_rot, cast_hit_pos - particle.x);

                var normal = new float3(0, 0, 0);
                if (local_pos.y >= 0.5f - 0.1f)
                {
                    normal.y = 1;
                }
                else if (local_pos.y <= -0.5f + 0.1f)
                {
                    normal.y = -1;
                }
                else if (local_pos.x >= 0.5f - 0.1f)
                {
                    normal.x = 1;
                }
                else if (local_pos.x <= -0.5f + 0.1f)
                {
                    normal.x = -1;
                }
                else if (local_pos.z >= 0.5f - 0.1f)
                {
                    normal.z = 1;
                }
                else if (local_pos.z <= -0.5f + 0.1f)
                {
                    normal.z = -1;
                }

                normal = math.normalizesafe(math.mul(p_rot, normal));

                cast_n_pos = particle.x + normal;

                if (Input.GetMouseButton(0))
                {
                    if (!left_click)
                    {
                        left_click = true;
                        world.RemoveParticle(cast.index);
                    }
                }
                else
                {
                    left_click = false;
                }
                if (Input.GetMouseButton(1))
                {

                    if (!right_click)
                    {
                       

                        right_click = true;
                        world.NewParticle(particle.x + normal, BlockRegistry.GLOW_JELLY);
                    }
                } else
                {
                    right_click = false;
                }
            }


            world.camera.transform.position = new(position.x, position.y + GetEyeHeight(), position.z);

            var rot = quaternion.AxisAngle(new(0, 1, 0), math.radians(yaw));
            world.camera.transform.rotation = Quaternion.AngleAxis(yaw, new(0, 1, 0)) * Quaternion.AngleAxis(pitch, new(1, 0, 0));

            if (locked)
            {
                if (!lastLocked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                yaw += Input.GetAxis("Mouse X");
                pitch -= Input.GetAxis("Mouse Y");


                float3 movement = float3.zero;
                if (Input.GetKey(KeyCode.W))
                {
                    movement += math.mul(rot, new float3(0, 0, 1));
                }
                if (Input.GetKey(KeyCode.S))
                {
                    movement += math.mul(rot, new float3(0, 0, -1));
                }
                if (Input.GetKey(KeyCode.A))
                {
                    movement += math.mul(rot, new float3(-1, 0, 0));
                }
                if (Input.GetKey(KeyCode.D))
                {
                    movement += math.mul(rot, new float3(1, 0, 0));
                }
                
                if (Input.GetKey(KeyCode.Space))
                {
                    if (in_water)
                    {
                        motion.y += Time.fixedDeltaTime * 7.0f;
                    }
                    else
                    {
                        if (on_ground)
                        {
                            motion.y = 7;
                        }
                    }
                }

                movement = math.normalizesafe(movement);

                motion.x = math.lerp(motion.x, movement.x * 3, Time.fixedDeltaTime * 8);
                motion.z = math.lerp(motion.z, movement.z * 3, Time.fixedDeltaTime * 8);
            } else
            {
                if (lastLocked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }

            lastLocked = locked;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                locked = !locked;
            }

        }
    }
}
