using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Tutorials.Tanks.Step6
{
    partial struct TankSpawningSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Execute.TankSpawning>();
            state.RequireForUpdate<Config>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var config = SystemAPI.GetSingleton<Config>();
            // This system will only run once, so the random seed can be hard-coded.
            // Using an arbitrary constant seed makes the behavior deterministic.
            var random = new Random(123);

            var query = SystemAPI.QueryBuilder().WithAll<URPMaterialPropertyBaseColor>().Build();
            // An EntityQueryMask provides an efficient test of whether a specific entity would
            // be selected by an EntityQuery.
            var queryMask = query.GetEntityQueryMask();

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var tanks = new NativeArray<Entity>(config.TankCount, Allocator.Temp);
            ecb.Instantiate(config.TankPrefab, tanks);

            foreach (var tank in tanks)
            {
                // Every root entity instantiated from a prefab has a LinkedEntityGroup component, which
                // is a list of all the entities that make up the prefab hierarchy.
                ecb.SetComponentForLinkedEntityGroup(tank, queryMask,
                    new URPMaterialPropertyBaseColor { Value = RandomColor(ref random) });
            }

            ecb.Playback(state.EntityManager);
        }

        // Helper to create any amount of colors as distinct from each other as possible.
        // See https://martin.ankerl.com/2009/12/09/how-to-create-random-colors-programmatically/
        static float4 RandomColor(ref Random random)
        {
            // 0.618034005f is inverse of the golden ratio
            var hue = (random.NextFloat() + 0.618034005f) % 1;
            return (Vector4)Color.HSVToRGB(hue, 1.0f, 1.0f);
        }
    }
}
