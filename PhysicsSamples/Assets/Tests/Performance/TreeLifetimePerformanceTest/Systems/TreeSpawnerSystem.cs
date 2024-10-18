// This system runs once on startup to initialize the TreeComponent data for each tree prefab and then deletes itself
// The number of trees spawned is calculated from the tree density and ground size. Apart from the forest of trees that
// are spawned, a single immortal tree and a single dead tree are also spawned.

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Unity.Physics
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    internal partial struct TreeSpawnerSystem : ISystem, ISystemStartStop
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TreeSpawnerComponent>();
        }

        public void OnStartRunning(ref SystemState state)
        {
            // set up the dead tree material
            var spawner = SystemAPI.GetSingleton<TreeSpawnerComponent>();
            GetDeadTreeMaterialIndex(state.EntityManager, spawner.TreeEntity, spawner.DeadTreeMaterial, out spawner.DeadTreeMaterialIndex);
            SystemAPI.SetSingleton(spawner);
        }

        public void OnStopRunning(ref SystemState state)
        {
        }

        static void GetDeadTreeMaterialIndex(EntityManager manager, Entity treePrefab, UnityObjectRef<UnityEngine.Material> deadTreeMaterial, out int deadTreeMaterialIndex)
        {
            deadTreeMaterialIndex = 0;
            // find tree top and register material
            if (manager.HasBuffer<LinkedEntityGroup>(treePrefab))
            {
                var leg = manager.GetBuffer<LinkedEntityGroup>(treePrefab);
                for (var j = 1; j < leg.Length; j++)
                {
                    var childEntity = leg[j].Value;
                    if (manager.HasComponent<TreeTopTag>(childEntity) && manager.HasComponent<RenderMeshArray>(childEntity))
                    {
                        var renderMeshArray = manager.GetSharedComponentManaged<RenderMeshArray>(childEntity);
                        var oldMaterialCount = renderMeshArray.MaterialReferences.Length;
                        var materials =
                            new UnityObjectRef<UnityEngine.Material>[oldMaterialCount + 1];
                        Array.Copy(renderMeshArray.MaterialReferences, materials, oldMaterialCount);
                        materials[oldMaterialCount] = deadTreeMaterial;

                        renderMeshArray = new RenderMeshArray(materials, renderMeshArray.MeshReferences,
                            renderMeshArray.MaterialMeshIndices);

                        manager.SetSharedComponentManaged(childEntity, renderMeshArray);

                        deadTreeMaterialIndex = MaterialMeshInfo.ArrayIndexToStaticIndex(oldMaterialCount);

                        break;
                    }
                }
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ProfilerMarker pm = new ProfilerMarker("Profile: TreeSpawnerSystem.OnUpdate"); //PROFILE
            pm.Begin(); //PROFILE

            var entityManager = state.EntityManager;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var creator = SystemAPI.GetSingletonRW<TreeSpawnerComponent>();

            // Set world size. To further increase size, modify Floor xz dimensions and the max cap.
            var groundSize = creator.ValueRO.GroundSize;
            float groundHalfSize = groundSize * 0.5f;

            // Determine the tree count based on the tree density
            var treeDensity = creator.ValueRO.TreeDensity;
            var treeCount = (int)math.round(groundSize * groundSize * treeDensity);
            Debug.Log($"Trees spawned: {treeCount}. Colliders spawned: {treeCount * 2}");

            using var spawnedEntities = new NativeArray<Entity>(treeCount, Allocator.Temp);
            ecb.Instantiate(creator.ValueRO.TreeEntity, spawnedEntities);

            var random = new Random(42);

            foreach (var tree in spawnedEntities)
            {
                float3 position = new float3(
                    random.NextFloat(-groundHalfSize, groundHalfSize),
                    0.0f,
                    random.NextFloat(-groundHalfSize, groundHalfSize));

                var growtime = random.NextFloat(1, creator.ValueRO.MaxGrowTime);
                var deadtime = random.NextFloat(0, creator.ValueRO.MaxDeadTime);
                var regrowdelay = random.NextFloat(1, creator.ValueRO.ReGrowDelay);

                var growInCounts = growtime / SystemAPI.Time.DeltaTime;
                var deadInCounts = deadtime / SystemAPI.Time.DeltaTime;
                var regrowInCounts = regrowdelay / SystemAPI.Time.DeltaTime;

                ecb.AddComponent(tree, new TreeComponent()
                {
                    SpawningPosition = position,
                    GrowTime = growtime,
                    DeadTime = deadtime,

                    GrowTimer = (int)growInCounts,
                    DeathTimer = (int)deadInCounts,
                    RegrowTimer = (int)regrowInCounts,
                    LifeCycleTracker = LifeCycleStates.IsGrowing
                });
                ecb.AddComponent(tree, new TreeState() { Value = TreeState.States.Default }); //ID the tree root
            }

            ecb.Playback(entityManager);
            ecb.Dispose();

            ecb = new EntityCommandBuffer(Allocator.Temp);
            // Perform a second pass to update the position of the Tree entities. Need to use the output from the first
            // ECB playback here.
            foreach (var(treeComponent, entity) in SystemAPI
                     .Query<RefRW<TreeComponent>>()
                     .WithEntityAccess())
            {
                var treeSpawnPosition = treeComponent.ValueRO.SpawningPosition;

                // Update for the prefab entity itself:
                var localTransform = entityManager.GetComponentData<LocalTransform>(entity);
                entityManager.SetComponentData(entity,  new LocalTransform
                {
                    Position =  treeSpawnPosition,
                    Scale = localTransform.Scale,
                    Rotation = localTransform.Rotation
                });

                if (entityManager.HasBuffer<LinkedEntityGroup>(entity))
                {
                    var leg = entityManager.GetBuffer<LinkedEntityGroup>(entity);

                    if (leg.Length > 1)
                    {
                        for (var j = 1; j < leg.Length; j++)
                        {
                            var childEntity = leg[j].Value;

                            ecb.AddComponent(childEntity, new TreeState() { Value = TreeState.States.Default});

                            var isJoint = entityManager.HasComponent<PhysicsJoint>(childEntity);
                            if (!isJoint)
                            {
                                var childPosition = entityManager.GetComponentData<LocalTransform>(childEntity);
                                var lt = new LocalTransform
                                {
                                    Position = treeSpawnPosition + childPosition.Position,
                                    Scale = childPosition.Scale,
                                    Rotation = childPosition.Rotation
                                };
                                entityManager.SetComponentData(childEntity, lt);
                                entityManager.SetComponentData(childEntity, new LocalToWorld { Value = lt.ToMatrix() });
                            }
                        }
                    }
                }
            }
            ecb.Playback(entityManager);
            ecb.Dispose();

            state.Enabled = false;

            pm.End(); //PROFILE
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
