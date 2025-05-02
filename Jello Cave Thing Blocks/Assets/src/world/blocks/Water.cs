using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;

namespace Assets.src.world.blocks
{
    internal class Water : Block
    {
        public override float4 GetColor()
        {
            return new(0.2f, 0.5f, 1.0f, 0.25f);
        }

        public override JelloWorld.ParticleType GetParticleType()
        {
            return JelloWorld.ParticleType.Water;
        }
    }
}
