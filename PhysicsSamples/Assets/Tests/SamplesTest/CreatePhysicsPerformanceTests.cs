using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Unity.Physics.Tests.PerformanceTests
{
    public class PrefabSpawnerPerformanceTest : MonoBehaviour
    {
        public GameObject SpawnedPrefab;
        public int Count = 600;
        public float ElementRadius = 1;

        class PrefabSpawnerTestBaker : Baker<PrefabSpawnerPerformanceTest>
        {
            public override void Bake(PrefabSpawnerPerformanceTest authoring)
            {
                DependsOn(authoring.SpawnedPrefab);
                var prefabEntity = GetEntity(authoring.SpawnedPrefab, TransformUsageFlags.Dynamic);
                if (prefabEntity == null)
                {
                    return;
                }

                var createComponent = new PrefabSpawnerPerformanceTestComponent
                {
                    Entity = prefabEntity,
                    Count = authoring.Count,
                    ElementRadius = authoring.ElementRadius,
                    SpawningPosition = authoring.transform.position
                };
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, createComponent);
            }
        }

        /// <summary>   Computes a list of position. </summary>
        ///
        /// <param name="count"> The number of desired positions
        /// <param name="paddingRadius"> The space between each position
        /// <param name="centerPosition"> Anchor around which the positions will be computed.
        ///
        /// <returns>   A list of positions (float3). </returns>
        internal static List<float3> ComputePositionsArray(int count, float paddingRadius, Vector3 centerPosition)
        {
            var arrayPositions = new List<float3>();

            int XCount = (int)Mathf.Sqrt(count);
            int ZCount = XCount;
            float xMax = (XCount - 1) * 0.5f * paddingRadius;
            float zMax = (XCount - 1) * 0.5f * paddingRadius;
            float xMin = -xMax;
            float zMin = -zMax;
            for (int i = 0; i < XCount; ++i)
            {
                for (int j = 0; j < ZCount; ++j)
                {
                    arrayPositions.Add(centerPosition + new Vector3(
                        xMin + i * paddingRadius, (i + j), zMin + j * paddingRadius));
                }
            }

            return arrayPositions;
        }
    }

    public struct PrefabSpawnerPerformanceTestComponent : IComponentData
    {
        public Entity Entity;
        public int Count;
        public float ElementRadius;
        public float3 SpawningPosition;
    }

    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [CreateAfter(typeof(PhysicsInitializeGroup))]
    [UpdateAfter(typeof(PhysicsInitializeGroup))]
    [UpdateBefore(typeof(PhysicsSimulationGroup))]
    internal partial struct CreatePerformanceTestSystem : ISystem
    {
        private EntityQuery PrefabSpawnerComponentQuery;
        public void OnCreate(ref SystemState state)
        {
            PrefabSpawnerComponentQuery = state.GetEntityQuery(typeof(PrefabSpawnerPerformanceTestComponent));
            state.RequireForUpdate(PrefabSpawnerComponentQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var(creator, creatorEntity) in SystemAPI.Query<RefRO<PrefabSpawnerPerformanceTestComponent>>()
                     .WithEntityAccess())
            {
                var positions = PrefabSpawnerPerformanceTest.ComputePositionsArray(creator.ValueRO.Count, creator.ValueRO.ElementRadius, creator.ValueRO.SpawningPosition);
                foreach (var position in positions)
                {
                    Entity entity = entityManager.Instantiate(creator.ValueRO.Entity);


                    var transform = entityManager.GetComponentData<LocalTransform>(entity);
                    transform.Position = position;
                    entityManager.SetComponentData(entity, transform);


                    ecb.AddComponent<PhysicsCollider>(entity);
                }

                ecb.DestroyEntity(creatorEntity);
            }
        }
    }
}
