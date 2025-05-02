using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;

namespace Assets.src.world.blocks
{

    internal class Stone : Block
    {
        public override float4 GetColor()
        {
            return new(0.5f, 0.5f, 0.5f, 1.0f);
        }

        public override float GetStiffness()
        {
            return 8.0f;
        }
    }
}
