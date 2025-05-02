using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;

namespace Assets.src.world.blocks
{
    internal class Leaves : Block
    {
        public override float4 GetColor()
        {
            return new(0.0f, 0.5f, 0.0f, 1.0f);
        }

        public override float GetMass()
        {
            return 0.7f;
        }

        public override float GetStiffness()
        {
            return 4.0f;
        }
    }
}
