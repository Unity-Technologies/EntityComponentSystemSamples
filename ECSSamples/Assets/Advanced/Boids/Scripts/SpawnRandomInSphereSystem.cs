using System.Collections.Generic;
using Samples.Boids;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Samples.Common
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public class SpawnRandomInSphereSystem : ComponentSystem
    {        
        protected override void OnUpdate()
        {
            Entities.ForEach((Entity e, SpawnRandomInSphere spawner, ref LocalToWorld localToWorld) =>                
            {
                int toSpawnCount = spawner.Count;

                // Using a .TempJob instead of a .Temp for `spawnPositions`, because the method
                // `RandomPointsInUnitSphere` passes this NativeArray into a Job
                var spawnPositions = new NativeArray<float3>(toSpawnCount, Allocator.TempJob);
                GeneratePoints.RandomPointsInUnitSphere(spawnPositions);

                // Calling Instantiate once per spawned Entity is rather slow, and not recommended
                // This code is placeholder until we add the ability to bulk-instantiate many entities from an ECB
                var entities = new NativeArray<Entity>(toSpawnCount, Allocator.Temp);
                for (int i = 0; i < toSpawnCount; ++i)
                {
                    entities[i] = PostUpdateCommands.Instantiate(spawner.Prefab);
                }

                for (int i = 0; i < toSpawnCount; i++)
                {
                    PostUpdateCommands.SetComponent(entities[i], new LocalToWorld
                    {
                        Value = float4x4.TRS(
                            localToWorld.Position + (spawnPositions[i] * spawner.Radius),
                            quaternion.LookRotationSafe(spawnPositions[i], math.up()),
                            new float3(1.0f, 1.0f, 1.0f))
                    });
                }

                // Using 'RemoveComponent' instead of 'DestroyEntity' as a safety.
                // Removing the SpawnRandomInSphere component is sufficient to prevent the spawner
                // from executing its spawn logic more than once. The spawner may have other components
                // that are relevant to ongoing processing; this system doesn't know about them & shouldn't
                // assume the entity is safe to delete.
                PostUpdateCommands.RemoveComponent<SpawnRandomInSphere>(e);

                spawnPositions.Dispose();
                entities.Dispose();
            });
        }
    }
}
