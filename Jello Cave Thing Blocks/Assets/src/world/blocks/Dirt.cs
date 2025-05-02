using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;

namespace Assets.src.world.blocks
{

    internal class Dirt : Block
    {
        public override float4 GetColor()
        {
            return new(0.8f, 0.4f, 0.35f, 1.0f);
        }
    }
}
