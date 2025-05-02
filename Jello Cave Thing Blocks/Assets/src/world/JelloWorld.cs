using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;
using System;
using JetBrains.Annotations;
using DotnetNoise;
using Unity.VisualScripting;
using Assets.src.world.blocks;
using Assets.src.world.entities;

public class JelloWorld : MonoBehaviour
{
    public const int grid_size = 64;
    public const int grid_padding = 4;
    public const int total_size = grid_size + grid_padding;

    public const int grid_height = 128;
    public const int total_height = grid_height + grid_padding;

    public Material DefaultMat;

    public struct EntityCollisionTest
    {
        public float4 pos;
        float2 padding;
        public float width;
        public float height;
    }

    public struct EntityCollisionResult
    {
        public int up, down, left, right, front, back, water, step_up;
    }

    public int max_entities = 256;
    public EntityCollisionTest[] tests = new EntityCollisionTest[256];
    public EntityCollisionResult[] results = new EntityCollisionResult[256];
    public List<Entity> entities = new();

    public new GameObject camera;

    public ComputeShader EntityCollisions;
    public ComputeShader clearGrid;
    public ComputeShader AddParticles;
    public ComputeShader InitParticles;
    public ComputeShader P2G;
    public ComputeShader P2G2;
    public ComputeShader UpdateGrid;
    public ComputeShader G2P;
    public ComputeShader UpdateFloodFill;
    public ComputeShader E2G;

    public struct EntityUpdate
    {
        public float3 x;
        public float mass;
        public float3 v;
        float padding;
    }


    public Material ParticleMat;
    public Material ParticleMat_T;
    public Mesh ParticleMesh;

    public struct Raycast
    {
        public float4 origin;
        public float4 direction;
    };

    public struct RaycastResult
    {
        public int distance;
        public int ID;
    };



    public struct AddParticle
    {
        public float3 x;
        public float stiffness;
        public float3 v;
        public float type;
        public float4 color;
        public float4 emission;
        public float mass;
        public int ID;
        public float2 padding;
    }

    public struct Particle
    {
        public float3 x; // 1, 2, 3
        public float mass; // 4

        public float3 v; // 1, 2, 3
        public float volume_0; // 4

        public float3x3 C; // 1, 2, 3, 4, 5, 6, 7, 8, 9
        public float3 p1; // 10, 11, 12 (x = type)

        public float3x3 F; // 1, 2, 3, 4, 5, 6, 7, 8, 9
        float3 p2; // 10, 11, 12

        float4 color; // 1, 2, 3, 4
    };

    struct GridCell
    {
        public float3 v;
        public float mass;
    };

    ComputeBuffer AddParticleBuf;
    ComputeBuffer RemoveParticleBuf;
    ComputeBuffer ParticleBuf;
    ComputeBuffer Grid;
    ComputeBuffer Occlusion;
    ComputeBuffer FloodFill;
    ComputeBuffer Emission;
    ComputeBuffer RaycastBuffer;
    ComputeBuffer RaycastResultBuffer;
    public ComputeBuffer EntityUpdateBuffer;

    List<AddParticle> adding = new();
    List<Block> blocksForParticles = new();

    int NumParticles = 0;

    Raycast[] raycasts = new Raycast[512];
    RaycastResult[] raycast_results = new RaycastResult[512];
    int NumRaycasts = 0;

    List<EntityUpdate> updates = new();

    ComputeBuffer EntityCollisionTests;
    ComputeBuffer EntityCollisionResults;

    public int AddRaycast(float3 origin, float3 direction)
    {
        raycasts[NumRaycasts].origin = new float4(origin.x, origin.y, origin.z, 0);
        raycasts[NumRaycasts].direction = new float4(direction.x, direction.y, direction.z, 0);
        NumRaycasts++;
        return NumRaycasts - 1;
    }

    public struct WorldRaycastResult
    {
        public float dist;
        public int index;
        public Particle particle;
    }

    public WorldRaycastResult GetRaycast(int id)
    {
        RaycastResult result = raycast_results[id];

        WorldRaycastResult r = new();
        r.dist = result.distance / 10000.0f;
        r.index = result.ID;
        Particle[] p = new Particle[1];
        if (result.ID != -1)
        {
            ParticleBuf.GetData(p, 0, result.ID, 1);
        }
        r.particle = p[0];

        return r;
    }

    public enum ParticleType
    {
        Jello, Water
    }


    List<int> RemovedParticles = new();

    public PlayerEntity player;

    void Start()
    {
        /*
        NumParticles = 64 * 256 * 64;

        for (int i = 0; i < NumParticles; i++)
        {
            RemovedParticles.Add(i);
            blocksForParticles.Add(null);
        }
         */

        player = new PlayerEntity(this);
        entities.Add(player);

        var r = new System.Random();
        for (int i = 0; i < 32; i++)
        {
            WormEntity worm = new(this);
            worm.position = new(32 + r.Next(-15, 15), 80 + r.Next(-15, 15), 32 + r.Next(-15, 15));
            entities.Add(worm);
        }

        FrogEntity frog = new(this);
        frog.position = new(32, 80, 32);
        entities.Add(frog);

        player.position = new(32, 80, 32);

        unsafe
        {
            AddParticleBuf = new ComputeBuffer(128 * 128 * 128, sizeof(AddParticle));
            RemoveParticleBuf = new ComputeBuffer(128 * 128 * 128, sizeof(int));
            ParticleBuf = new ComputeBuffer(128 * 128 * 128, sizeof(Particle));
            Grid = new ComputeBuffer(total_size * total_size * total_height, sizeof(GridCell));
            Occlusion = new ComputeBuffer(total_size * total_size * total_height, sizeof(float2));
            FloodFill = new ComputeBuffer(total_size * total_size * total_height, sizeof(float4));
            Emission = new ComputeBuffer(total_size * total_size * total_height, sizeof(float4));
            EntityCollisionTests = new ComputeBuffer(256, sizeof(EntityCollisionTest));
            EntityCollisionResults = new ComputeBuffer(256, sizeof(EntityCollisionResult));
            RaycastBuffer = new ComputeBuffer(512, sizeof(Raycast));
            RaycastResultBuffer = new ComputeBuffer(512, sizeof(RaycastResult));
            EntityUpdateBuffer = new ComputeBuffer(256, sizeof(EntityUpdate));

            GridCell[] cell = new GridCell[Grid.count];
            for (int i = 0; i < Grid.count; i++)
            {
                cell[i].v = 0;
                cell[i].mass = 0;
            }
            Grid.SetData(cell);

            int[] removing = new int[128 * 128 * 128];
            for (int i = 0; i < removing.Length; i++)
            {
                removing[i] = 1;
            }
            RemoveParticleBuf.SetData(removing);

            Particle[] p = new Particle[128 * 128 * 128];
            ParticleBuf.SetData(p);

            AddParticle[] a = new AddParticle[128 * 128 * 128];
            AddParticleBuf.SetData(a);
        }

        P2G.SetBuffer(0, "Emission", Emission);

        EntityCollisions.SetBuffer(0, "Particles", ParticleBuf);
        EntityCollisions.SetBuffer(0, "Removed", RemoveParticleBuf);
        EntityCollisions.SetBuffer(0, "Tests", EntityCollisionTests);
        EntityCollisions.SetBuffer(0, "Results", EntityCollisionResults);
        EntityCollisions.SetBuffer(0, "Raycasts", RaycastBuffer);
        EntityCollisions.SetBuffer(0, "RaycastResults", RaycastResultBuffer);

        E2G.SetInt("grid_size", grid_size);
        E2G.SetInt("grid_padding", grid_padding);
        E2G.SetInt("grid_height", grid_height);
        E2G.SetBuffer(0, "Entities", EntityUpdateBuffer);
        E2G.SetBuffer(0, "Grid", Grid);


        UpdateFloodFill.SetBuffer(0, "Occlusion", Occlusion);
        UpdateFloodFill.SetBuffer(0, "FloodFill", FloodFill);
        UpdateFloodFill.SetBuffer(0, "Emission", Emission);
        UpdateFloodFill.SetInt("grid_size", grid_size);
        UpdateFloodFill.SetInt("grid_padding", grid_padding);
        UpdateFloodFill.SetInt("grid_height", grid_height);
        ParticleMat.SetBuffer("FloodFill", FloodFill);
        ParticleMat_T.SetBuffer("FloodFill", FloodFill);
        ParticleMat.SetInt("total_size", total_size);
        ParticleMat.SetInt("total_height", total_height);
        ParticleMat_T.SetInt("total_size", total_size);
        ParticleMat_T.SetInt("total_height", total_height);

        clearGrid.SetBuffer(0, "Emission", Emission);
        clearGrid.SetBuffer(0, "Grid", Grid);
        clearGrid.SetBuffer(0, "Occlusion", Occlusion);
        InitParticles.SetBuffer(0, "Grid", Grid);
        P2G.SetBuffer(0, "Grid", Grid);
        UpdateGrid.SetBuffer(0, "Grid", Grid);
        G2P.SetBuffer(0, "Grid", Grid);
        P2G2.SetBuffer(0, "Grid", Grid);
        P2G.SetBuffer(0, "Occlusion", Occlusion);

        AddParticles.SetBuffer(0, "AddingParticles", AddParticleBuf);
        AddParticles.SetBuffer(0, "Removed", RemoveParticleBuf);

        P2G.SetBuffer(0, "Particles", ParticleBuf);
        P2G2.SetBuffer(0, "Particles", ParticleBuf);
        InitParticles.SetBuffer(0, "Particles", ParticleBuf);
        G2P.SetBuffer(0, "Particles", ParticleBuf);
        AddParticles.SetBuffer(0, "Particles", ParticleBuf);
        ParticleMat.SetBuffer("Particles", ParticleBuf);
        ParticleMat_T.SetBuffer("Particles", ParticleBuf);

        P2G.SetBuffer(0, "Removed", RemoveParticleBuf);
        P2G2.SetBuffer(0, "Removed", RemoveParticleBuf);
        G2P.SetBuffer(0, "Removed", RemoveParticleBuf);
        ParticleMat.SetBuffer("Removed", RemoveParticleBuf);
        ParticleMat_T.SetBuffer("Removed", RemoveParticleBuf);

        G2P.SetInt("grid_size", grid_size);
        G2P.SetInt("grid_padding", grid_padding);
        G2P.SetInt("grid_height", grid_height);
        InitParticles.SetInt("grid_size", grid_size);
        InitParticles.SetInt("grid_padding", grid_padding);
        InitParticles.SetInt("grid_height", grid_height);
        P2G.SetInt("grid_height", grid_height);
        P2G.SetInt("grid_size", grid_size);
        P2G.SetInt("grid_padding", grid_padding);
        P2G2.SetInt("grid_size", grid_size);
        P2G2.SetInt("grid_padding", grid_padding);
        P2G2.SetInt("grid_height", grid_height);
        UpdateGrid.SetInt("grid_size", grid_size);
        UpdateGrid.SetInt("grid_padding", grid_padding);
        UpdateGrid.SetInt("grid_height", grid_height);
        clearGrid.SetInt("grid_size", grid_size);
        clearGrid.SetInt("grid_padding", grid_padding);
        clearGrid.SetInt("grid_height", grid_height);

        var rand = new System.Random();
        FastNoise noise = new(rand.Next());

        Block[,,] blocks = new Block[grid_size, grid_height, grid_size];

        Block GetBlock(int x, int y, int z)
        {
            if (x < 1 || y < 1 || z < 1 || x >= grid_size - 1 || y >= grid_height - 1 || z >= grid_size - 1)
            {
                return null;
            }
            return blocks[x, y, z];
        }

        void SetBlock(int x, int y, int z, Block block)
        {
            if (x < 1 || y < 1 || z < 1 || x >= grid_size - 1 || y >= grid_height - 1 || z >= grid_size - 1)
            {
                return;
            }
            blocks[x, y, z] = block;
        }

        for (int x = 0; x < grid_size; x++)
        {
            for (int z = 0; z < grid_size; z++)
            {

                float H = noise.GetPerlinFractal(x * 3, z * 3) * 25 + 64;
                int IH = (int)H;
                for (int y = 0; y < grid_height; y++)
                {
                    float var = (float)new System.Random().NextDouble() * 0.2f + 0.8f;

                    var caves = noise.GetPerlinFractal(x * 5, y * 5, z * 5) > 0.2f;

                    if (x <= 1 || z <= 1 || x >= grid_size - 2 || z >= grid_size - 2)
                    {
                        caves = false;
                        if (IH < 64)
                        {
                            IH = 64;
                        }
                    }

                    if (y <= 1)
                    {
                        SetBlock(x, y, z, BlockRegistry.FLOOR);
                    }
                    else
                    {
                        if (!caves)
                        {
                            if (y < IH)
                            {
                                if (y < IH - 4)
                                {
                                    SetBlock(x, y, z, BlockRegistry.STONE);
                                }
                                else
                                {
                                    SetBlock(x, y, z, BlockRegistry.DIRT);
                                }
                            }
                            else if (y == IH)
                            {
                                SetBlock(x, y, z, BlockRegistry.GRASS);
                            }
                        }
                        if (y > IH && y < 64)
                        {
                            SetBlock(x, y, z, BlockRegistry.WATER);
                        }
                    }
                }
            }
        }

        void SpawnTree(int x, int y, int z)
        {
            int height = rand.Next(4, 6);
            int start = y;

            for (int i = -1; i <= height; i++)
            {
                SetBlock(x - 1, y + i, z, BlockRegistry.LOG);
                SetBlock(x + 1, y + i, z, BlockRegistry.LOG);
                SetBlock(x, y + i, z - 1, BlockRegistry.LOG);
                SetBlock(x, y + i, z + 1, BlockRegistry.LOG);
                SetBlock(x, y + i, z, BlockRegistry.LOG);
            }
            y += height;
            for (int i = -2; i <= 2; i++)
            {
                for (int j = -2; j <= 2; j++)
                {
                    SetBlock(x + i, y, z + j, BlockRegistry.LEAVES);
                    SetBlock(x + i, y + 1, z + j, BlockRegistry.LEAVES);
                    if (i == 0 || j == 0)
                    {
                        SetBlock(x + i, y + 2, z + j, BlockRegistry.LEAVES);
                    }
                }
            }
        }

        for (int x = 0; x < grid_size; x++)
        {
            for (int z = 0; z < grid_size; z++)
            {
                for (int y = 0; y < grid_height; y++)
                {
                    var block = GetBlock(x, y, z);
                    if (block == BlockRegistry.GRASS)
                    {
                        if (GetBlock(x, y + 1, z) == BlockRegistry.WATER)
                        {
                            SetBlock(x, y, z, BlockRegistry.GRAVEL);
                        } else
                        {
                            if (rand.Next(100) <= 1)
                            {
                                SpawnTree(x, y, z);
                            }
                        }
                        
                    }
                    if (y < 8 && block == null)
                    {
                        SetBlock(x, y, z, BlockRegistry.LAVA);
                    }
                }
            }
        }

        for (int x = 0; x < grid_size; x++)
        {
            for (int z = 0; z < grid_size; z++)
            {
                for (int y = 0; y < grid_height; y++)
                {
                    var block = GetBlock(x, y, z);
                    if (block != null)
                    {
                        NewParticle(new(x + 2, y, z + 2), block);
                    }
                }
            }
        }
    }


    public void NewParticle(float3 position, Block block)
    {
        AddParticle p = new();
        p.x = position;
        p.stiffness = block.GetStiffness();
        p.v = new(0, 0, 0);
        p.color = block.GetColor();
        p.type = (int)block.GetParticleType();
        p.emission = block.GetEmission();
        p.mass = block.GetMass();

        if (RemovedParticles.Count > 0)
        {
            p.ID = RemovedParticles[0];
            RemovedParticles.RemoveAt(0);
            blocksForParticles[p.ID] = block;


            uint[] i = new uint[1];
            i[0] = 0;
            RemoveParticleBuf.SetData(i, 0, p.ID, 1);
        } else
        {
            p.ID = NumParticles++;
            blocksForParticles.Add(block);
        }

        adding.Add(p);
        UpdateNewParticles = true;
    }

    public void RemoveParticle(int id)
    {
        uint[] i = new uint[1];
        i[0] = 1;
        RemoveParticleBuf.SetData(i, 0, id, 1);
        RemovedParticles.Add(id);
        blocksForParticles[id] = null;
        UpdateNewParticles = true;
    }

    public float3x3 PolarDecomposition(float3x3 F)
    {
        // Normalize F
        float3x3 R = F;

        // Newton-Schulz Iteration for orthogonalization
        // Typically 3–5 iterations are enough
        for (int i = 0; i < 5; ++i)
        {
            float3x3 R_T = math.transpose(R);
            float3x3 R_invT = 0.5f * (3.0f * R - math.mul(R, math.mul(R_T, R)));
            R = R_invT;
        }

        return math.inverse(R); // Rotation matrix
    }

    void Update()
    {
        if (NumParticles == 0 && adding.Count == 0)
        {
            return;
        }

        RunSimulationStep(2);

        Graphics.DrawMeshInstancedProcedural(ParticleMesh, 0, ParticleMat, new Bounds(new(0, 0, 0), new(4096, 4096, 4096)), NumParticles);
        Graphics.DrawMeshInstancedProcedural(ParticleMesh, 0, ParticleMat_T, new Bounds(new(0, 0, 0), new(4096, 4096, 4096)), NumParticles);

        var player = (PlayerEntity)(entities[0]);
        Graphics.DrawMesh(ParticleMesh, Matrix4x4.Translate(new(player.cast_hit_pos.x, player.cast_hit_pos.y, player.cast_hit_pos.z)) * Matrix4x4.Scale(new(0.2f, 0.2f, 0.2f)), DefaultMat, 0);
        Graphics.DrawMesh(ParticleMesh, Matrix4x4.Translate(new(player.cast_n_pos.x, player.cast_n_pos.y, player.cast_n_pos.z)) * Matrix4x4.Scale(new(0.2f, 0.2f, 0.2f)), DefaultMat, 0);
    
        for (int i = 0; i < entities.Count; i++)
        {
            entities[i].Render();
        }
    }

    private bool UpdateNewParticles;
    public void RunSimulationStep(int steps = 3)
    {
        for (int i = 0; i < steps; i++)
        {
            if (NumParticles == 0 && adding.Count == 0)
            {
                return;
            }

            clearGrid.Dispatch(0, total_size / 4, total_height / 4, total_size / 4);

            if (UpdateNewParticles)
            {
                UpdateNewParticles = false;

                AddParticleBuf.SetData(adding);
                AddParticles.SetInt("NumAdding", adding.Count);

                P2G.SetInt("NumParticles", NumParticles);
                EntityCollisions.SetInt("NumParticles", NumParticles);
                AddParticles.Dispatch(0, math.max(adding.Count / 64, 1), 1, 1); // add particles to the end of the list
                adding.Clear();
                P2G.SetInt("Start", 0);
                P2G.SetInt("End", NumParticles);
                P2G.Dispatch(0, math.max(1, NumParticles / 64), 1, 1); // Transfer the particles to the grid

                P2G2.SetInt("End", NumParticles);
                P2G2.SetInt("Start", 0);
                P2G2.SetInt("NumParticles", NumParticles);

                G2P.SetInt("NumParticles", NumParticles);
                G2P.SetInt("Start", 0);
                G2P.SetInt("End", NumParticles);

                InitParticles.SetInt("End", NumParticles);
                InitParticles.SetInt("NumParticles", NumParticles);
                InitParticles.Dispatch(0, math.max(1, NumParticles / 64), 1, 1); // Set the initial volume

                clearGrid.Dispatch(0, total_size / 4, total_height / 4, total_size / 4); // Clear grid again
            }



            P2G.Dispatch(0, math.max(1, NumParticles / 64), 1, 1);

            P2G2.Dispatch(0, math.max(1, NumParticles / 64), 1, 1);

            E2G.Dispatch(0, math.max(1, updates.Count / 64), 1, 1);

            UpdateGrid.Dispatch(0, total_size / 4, total_height / 4, total_size / 4);

            G2P.Dispatch(0, math.max(1, NumParticles / 64), 1, 1);

            UpdateFloodFill.Dispatch(0, total_size / 4, total_height / 4, total_size / 4);
        }
    }

    private void FixedUpdate()
    {
        if (NumParticles == 0)
        {
            return;
        }

        NumRaycasts = 0;

        updates.Clear();
        for (int i = 0; i < entities.Count; i++)
        {
            var pos = entities[i].position;
            tests[i].pos = new float4(pos.x, pos.y, pos.z, 0);
            tests[i].width = entities[i].width;
            tests[i].height = entities[i].height;

            entities[i].CastRays();


            EntityUpdate u = new();
            u.x = entities[i].position;
            u.v = entities[i].motion;
            u.mass = 1.0f;
            updates.Add(u);
        }

        EntityUpdateBuffer.SetData(updates);
        E2G.SetInt("NumEntities", updates.Count);
        E2G.SetFloat("fixed_delta", 0.05f);

        for (int i = 0; i < NumRaycasts; i++)
        {
            raycast_results[i].distance = 999999999;
            raycast_results[i].ID = -1;
        }

        EntityCollisionTests.SetData(tests);
        EntityCollisionResults.SetData(new EntityCollisionResult[max_entities]);

        RaycastBuffer.SetData(raycasts);
        RaycastResultBuffer.SetData(raycast_results);

        EntityCollisions.SetInt("NumEntities", entities.Count);
        EntityCollisions.SetInt("NumRaycasts", NumRaycasts);
        EntityCollisions.Dispatch(0, NumParticles / 64, 1, 1);

        EntityCollisionResults.GetData(results);
        RaycastResultBuffer.GetData(raycast_results);

        for (int i = 0; i < entities.Count; i++)
        {
            entities[i].FixedUpdate();
            entities[i].DoPhysics(i);
        }
    }

    private void OnDestroy()
    {
        ParticleBuf.Dispose();
        RemoveParticleBuf.Dispose();
        AddParticleBuf.Dispose();
    }

}
