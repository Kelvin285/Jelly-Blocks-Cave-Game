using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.src.world.entities
{
    public class FrogEntity : Entity
    {
        public Material Green;
        public Material Yellow;
        public FrogEntity(JelloWorld world) : base(world)
        {
            Green = new Material(world.DefaultMat);
            Green.color = Color.green;

            Yellow = new Material(world.DefaultMat);
            Yellow.color = Color.yellow;
            no_gravity = true;
        }



        public override void Render()
        {
            base.Render();
            DrawCube(position, 1, quaternion.identity, Green);
        }
    }
}
