using Tutorials.Tanks.Execute;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Tutorials.Tanks.Step6
{
    [BurstCompile]
    partial struct TankSpawningSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TankSpawning>();
            state.RequireForUpdate<Config>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();

            // This system will only run once, so the random seed can be hard-coded.
            // Using an arbitrary constant seed makes the behavior deterministic.
            var random = Random.CreateFromIndex(1234);
            var hue = random.NextFloat();

            // Helper to create any amount of colors as distinct from each other as possible.
            // The logic behind this approach is detailed at the following address:
            // https://martin.ankerl.com/2009/12/09/how-to-create-random-colors-programmatically/
            URPMaterialPropertyBaseColor RandomColor()
            {
                // Note: if you are not familiar with this concept, this is a "local function".
                // You can search for that term on the internet for more information.

                // 0.618034005f == 2 / (math.sqrt(5) + 1) == inverse of the golden ratio
                hue = (hue + 0.618034005f) % 1;
                var color = Color.HSVToRGB(hue, 1.0f, 1.0f);
                return new URPMaterialPropertyBaseColor { Value = (Vector4)color };
            }

            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var vehicles = CollectionHelper.CreateNativeArray<Entity>(config.TankCount, Allocator.Temp);
            ecb.Instantiate(config.TankPrefab, vehicles);

            var query = SystemAPI.QueryBuilder().WithAll<URPMaterialPropertyBaseColor>().Build();
            // An EntityQueryMask provides an efficient test of whether a specific entity would
            // be selected by an EntityQuery.
            var queryMask = query.GetEntityQueryMask();

            foreach (var vehicle in vehicles)
            {
                // Every prefab root contains a LinkedEntityGroup, a list of all its entities.
                ecb.SetComponentForLinkedEntityGroup(vehicle, queryMask, RandomColor());
            }

            state.Enabled = false;
        }
    }
}
