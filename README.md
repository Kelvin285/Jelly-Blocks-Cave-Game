Experimental project using a GPU-Based MLS/MPM simulation
The goal of the project was to see if I could create a playable game using a GPU-based particle simulation
The physics in the game are simple AABB collisions aside from the particle simulation.  You can place and break blocks similarly to Minecraft.
W, A, S, and D to move,
Left-Click to break blocks,
Right-Click to place blocks

Current Features:
- MLS/MPM particle system for terrain (not constraint-based currently so forces need to be clamped to prevent the simulation from exploding)
- AABB collisions between entities and terrain
- Raycasting
- GPU-based cellular-automata lighting

Ideas for future development:
- Final boss
- Multiple locations (travel via map and/or portal)
- Saving/Loading
- Menu
- Survival Mode (health bar + enemies)


WIP Character AI:
- Worms (AI finished.  Does not spawn in the world yet)
- Frogs (Started working on model.  No AI yet)
- Moles (No model or AI.  Currently only a plan for later)


![image](https://github.com/user-attachments/assets/8123f001-5dbb-4753-bc3e-2db1aafbc16d)

