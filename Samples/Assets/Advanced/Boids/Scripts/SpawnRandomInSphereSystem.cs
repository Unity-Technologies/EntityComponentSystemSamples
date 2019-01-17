using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Samples.Common
{
    public class SpawnRandomInSphereSystem : ComponentSystem
    {
        struct SpawnRandomInSphereInstance
        {
            public int spawnerIndex;
            public Entity sourceEntity;
            public float3 position;
        }

        ComponentGroup m_MainGroup;

        protected override void OnCreateManager()
        {
            m_MainGroup = GetComponentGroup(typeof(SpawnRandomInSphere), typeof(Position));
        }

        protected override void OnUpdate()
        {
            var uniqueTypes = new List<SpawnRandomInSphere>(10);

            EntityManager.GetAllUniqueSharedComponentData(uniqueTypes);

            int spawnInstanceCount = 0;
            for (int sharedIndex = 0; sharedIndex != uniqueTypes.Count; sharedIndex++)
            {
                var spawner = uniqueTypes[sharedIndex];
                m_MainGroup.SetFilter(spawner);
                var entityCount = m_MainGroup.CalculateLength();
                spawnInstanceCount += entityCount;
            }

            if (spawnInstanceCount == 0)
                return;

            var spawnInstances = new NativeArray<SpawnRandomInSphereInstance>(spawnInstanceCount, Allocator.Temp);
            {
                int spawnIndex = 0;
                for (int sharedIndex = 0; sharedIndex != uniqueTypes.Count; sharedIndex++)
                {
                    var spawner = uniqueTypes[sharedIndex];
                    m_MainGroup.SetFilter(spawner);

                    if (m_MainGroup.CalculateLength() == 0)
                        continue;
 
                    var entities = m_MainGroup.ToEntityArray(Allocator.TempJob);
                    var positions = m_MainGroup.ToComponentDataArray<Position>(Allocator.TempJob);

                    for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
                    {
                        var spawnInstance = new SpawnRandomInSphereInstance();

                        spawnInstance.sourceEntity = entities[entityIndex];
                        spawnInstance.spawnerIndex = sharedIndex;
                        spawnInstance.position = positions[entityIndex].Value;

                        spawnInstances[spawnIndex] = spawnInstance;
                        spawnIndex++;
                    }

                    entities.Dispose();
                    positions.Dispose();
                }
            }

            for (int spawnIndex = 0; spawnIndex < spawnInstances.Length; spawnIndex++)
            {
                int spawnerIndex = spawnInstances[spawnIndex].spawnerIndex;
                var spawner = uniqueTypes[spawnerIndex];
                int count = spawner.count;
                var entities = new NativeArray<Entity>(count,Allocator.Temp);
                var prefab = spawner.prefab;
                float radius = spawner.radius;
                var spawnPositions = new NativeArray<float3>(count, Allocator.Temp);
                float3 center = spawnInstances[spawnIndex].position;
                var sourceEntity = spawnInstances[spawnIndex].sourceEntity;

                GeneratePoints.RandomPointsInSphere(center,radius,ref spawnPositions);

                EntityManager.Instantiate(prefab, entities);

                for (int i = 0; i < count; i++)
                {
                    var position = new Position
                    {
                        Value = spawnPositions[i]
                    };
                    EntityManager.SetComponentData(entities[i],position);
                }

                EntityManager.RemoveComponent<SpawnRandomInSphere>(sourceEntity);

                spawnPositions.Dispose();
                entities.Dispose();
            }
            spawnInstances.Dispose();
        }
    }
}