using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;

namespace Assets.src.world.blocks
{
    internal class Floor : Block
    {
        public override float4 GetColor()
        {
            return new(0.45f, 0.35f, 0.35f, 1.0f);
        }

        public override float GetStiffness()
        {
            return 8.0f;
        }
    }
}
