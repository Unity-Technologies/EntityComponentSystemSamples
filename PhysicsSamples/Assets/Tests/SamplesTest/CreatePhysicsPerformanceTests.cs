using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
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

                var createComponent = new PrefabSpawnerPerformanceTestComponent
                {
                    Entity = prefabEntity,
                    Count = authoring.Count,
                    ElementRadius = authoring.ElementRadius,
                    SpawningPosition = authoring.transform.position
                };
                var entity = GetEntity(TransformUsageFlags.None);
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

            int XCount = (int)math.ceil(Mathf.Sqrt(count));
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

                    if (arrayPositions.Count >= count) break;
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
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    internal partial struct CreatePerformanceTestSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PrefabSpawnerPerformanceTestComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var entityManager = state.World.EntityManager;

            foreach (var creator in
                     SystemAPI.Query<RefRO<PrefabSpawnerPerformanceTestComponent>>())
            {
                var initialTransform = entityManager.GetComponentData<LocalTransform>(creator.ValueRO.Entity);
                var positions = PrefabSpawnerPerformanceTest.ComputePositionsArray(
                    creator.ValueRO.Count, creator.ValueRO.ElementRadius, creator.ValueRO.SpawningPosition);

                var spawnedEntities = entityManager.Instantiate(
                    creator.ValueRO.Entity, creator.ValueRO.Count, state.WorldUpdateAllocator);

                var i = 0;
                foreach (var s in spawnedEntities)
                {
                    entityManager.SetComponentData(s , new LocalTransform
                    {
                        Position = positions[i],
                        Scale = initialTransform.Scale,
                        Rotation = initialTransform.Rotation
                    });
                    i++;
                }
            }

            state.Enabled = false;
        }

        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
