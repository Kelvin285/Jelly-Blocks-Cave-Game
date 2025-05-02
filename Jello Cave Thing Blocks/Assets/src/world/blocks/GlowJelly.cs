using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;

namespace Assets.src.world.blocks
{
    internal class GlowJelly : Block
    {
        public override float4 GetColor()
        {
            return new(1.0f, 0.8f, 0.0f, 1.0f);
        }

        public override JelloWorld.ParticleType GetParticleType()
        {
            return JelloWorld.ParticleType.Jello;
        }

        public override float4 GetEmission()
        {
            return new(1.0f, 0.8f, 0.0f, 1.0f);
        }
    }
}
