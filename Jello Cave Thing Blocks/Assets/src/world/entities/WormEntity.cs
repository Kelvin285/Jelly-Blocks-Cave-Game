using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.src.world.entities
{
    public class WormEntity : Entity
    {
        public float3[] worm_pos;

        public float worm_size = 0.5f;
        public int worm_length = 5;
        public Material WormMat;

        public WormEntity(JelloWorld world) : base(world)
        {
            no_physics = true;
            worm_pos = new float3[worm_length];

            WormMat = new Material(world.DefaultMat);
            WormMat.color = Color.red;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (on_ground)
            {
                const float accel = 10.0f;
                const float max_vel = 10.0f;

                var player = world.player;

                var target_pos = player.position;

                var target_dir = target_pos - position;

                var target_sign = math.sign(target_dir);

                motion += accel * Time.fixedDeltaTime * target_sign;

                var abs_motion = math.abs(motion);

                motion -= math.sign(motion) * (float3)(abs_motion > max_vel) * (accel * 0.5f);

            } else
            {
                motion.y -= 10.0f * Time.fixedDeltaTime;
            }

            for (int i = 0; i < worm_pos.Length; i++)
            {
                float3 target = 0;
                if (i == 0)
                {
                    target = position;
                } else
                {
                    target = worm_pos[i - 1];
                }
                if (math.distance(worm_pos[i], target) > worm_size)
                {
                    float3 dir = math.normalizesafe(worm_pos[i] - target) * worm_size;
                    worm_pos[i] = target + dir;
                }
            }
        }

        public override void Render()
        {
            base.Render();

            DrawCube(position, worm_size, quaternion.identity, WormMat);

            for (int i = 0; i < worm_length; i++)
            {
                DrawCube(worm_pos[i], worm_size, quaternion.identity, WormMat);
            }
        }
    }
}
