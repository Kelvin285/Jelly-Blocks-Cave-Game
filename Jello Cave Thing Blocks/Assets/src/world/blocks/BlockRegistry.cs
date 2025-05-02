using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.src.world.blocks
{
    internal class BlockRegistry
    {
        public static List<Block> blocks = new();

        public static Block RegisterBlock(Block block)
        {
            blocks.Add(block);
            return block;
        }

        public static Dirt DIRT = (Dirt)RegisterBlock(new Dirt());
        public static Grass GRASS = (Grass)RegisterBlock(new Grass());
        public static Floor FLOOR = (Floor)RegisterBlock(new Floor());
        public static Lava LAVA = (Lava)RegisterBlock(new Lava());
        public static Stone STONE = (Stone)RegisterBlock(new Stone());
        public static Water WATER = (Water)RegisterBlock(new Water());
        public static Log LOG = (Log)RegisterBlock(new Log());
        public static Leaves LEAVES = (Leaves)RegisterBlock(new Leaves());
        public static Gravel GRAVEL = (Gravel)RegisterBlock(new Gravel());
        public static Obsidian OBSIDIAN = (Obsidian)RegisterBlock(new Obsidian());
        public static GlowJelly GLOW_JELLY = (GlowJelly)RegisterBlock(new GlowJelly());
    }
}
