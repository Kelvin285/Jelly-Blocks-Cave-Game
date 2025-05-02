using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;

namespace Assets.src.world.blocks
{
    public class Block
    {
        public int ID;

        private static int T_ID = 0;
        public Block()
        {
            ID = T_ID++;
        }
        public virtual float GetStiffness()
        {
            return 4.0f;
        }

        public virtual float4 GetColor()
        {
            return 1.0f;
        }

        public virtual JelloWorld.ParticleType GetParticleType()
        {
            return JelloWorld.ParticleType.Jello;
        }

        public virtual float4 GetEmission()
        {
            return 0;
        }

        public virtual float GetMass()
        {
            return 1.0f;
        }
    }
}
