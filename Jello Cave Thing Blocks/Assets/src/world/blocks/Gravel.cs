using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;

namespace Assets.src.world.blocks
{

    internal class Gravel : Block
    {
        public override float4 GetColor()
        {
            return new(0.35f, 0.6f, 0.4f, 1.0f);
        }

        public override float GetStiffness()
        {
            return 2.0f;
        }
    }
}
