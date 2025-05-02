using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;

namespace Assets.src.world.blocks
{
    internal class Lava : Block
    {
        public override float4 GetColor()
        {
            return new(1.0f, 106.0f / 255.0f, 0.0f, 0.9F);
        }

        public override JelloWorld.ParticleType GetParticleType()
        {
            return JelloWorld.ParticleType.Water;
        }

        public override float4 GetEmission()
        {
            return new(1.0f, 0.0f, 0.0f, 1.0f);
        }
    }
}
