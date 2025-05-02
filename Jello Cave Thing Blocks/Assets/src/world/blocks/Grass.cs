using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;

namespace Assets.src.world.blocks
{
    internal class Grass : Block
    {
        public override float4 GetColor()
        {
            return new(0.0f, 1.0f, 0.0f, 1.0f);
        }
    }
}
