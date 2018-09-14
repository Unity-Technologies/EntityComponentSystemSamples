using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Samples.Common
{
    public class SpawnChainSystem : ComponentSystem
    {
        struct SpawnChainInstance
        {
            public int spawnerIndex;
            public Entity sourceEntity;
            public float3 position;
        }

        ComponentGroup m_MainGroup;

        protected override void OnCreateManager()
        {
            m_MainGroup  = GetComponentGroup(typeof(SpawnChain),typeof(Position));
        }

        protected override void OnUpdate()
        {
            var uniqueTypes = new List<SpawnChain>(10);
            EntityManager.GetAllUniqueSharedComponentData(uniqueTypes);

            int spawnInstanceCount = 0;
            for (int sharedIndex = 0; sharedIndex != uniqueTypes.Count; sharedIndex++)
            {
                var spawner = uniqueTypes[sharedIndex];
                m_MainGroup.SetFilter(spawner);
                var entities = m_MainGroup.GetEntityArray();
                spawnInstanceCount += entities.Length;
            }

            if (spawnInstanceCount == 0)
                return;

            var spawnInstances = new NativeArray<SpawnChainInstance>(spawnInstanceCount, Allocator.Temp);
            {
                int spawnIndex = 0;
                for (int sharedIndex = 0; sharedIndex != uniqueTypes.Count; sharedIndex++)
                {
                    var spawner = uniqueTypes[sharedIndex];
                    m_MainGroup.SetFilter(spawner);
                    var entities = m_MainGroup.GetEntityArray();
                    var positions = m_MainGroup.GetComponentDataArray<Position>();

                    for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
                    {
                        var spawnInstance = new SpawnChainInstance();

                        spawnInstance.sourceEntity = entities[entityIndex];
                        spawnInstance.spawnerIndex = sharedIndex;
                        spawnInstance.position = positions[entityIndex].Value;

                        spawnInstances[spawnIndex] = spawnInstance;
                        spawnIndex++;
                    }
                }
            }

            for (int spawnIndex = 0; spawnIndex < spawnInstances.Length; spawnIndex++)
            {
                int spawnerIndex = spawnInstances[spawnIndex].spawnerIndex;
                var spawner = uniqueTypes[spawnerIndex];
                int count = spawner.count;
                var entities = new NativeArray<Entity>(count,Allocator.Temp);
                var prefab = spawner.prefab;
                float minDistance = spawner.minDistance;
                float maxDistance = spawner.maxDistance;
                float3 center = spawnInstances[spawnIndex].position;
                var sourceEntity = spawnInstances[spawnIndex].sourceEntity;

                EntityManager.Instantiate(prefab, entities);

                {
                    PositionConstraint contraint = new PositionConstraint();

                    contraint.parentEntity = sourceEntity;
                    contraint.maxDistance = maxDistance;

                    EntityManager.AddComponentData(entities[0],contraint);
                }

                for (int i = 1; i < count; i++ )
                {
                    PositionConstraint contraint = new PositionConstraint();

                    contraint.parentEntity = entities[i - 1];
                    contraint.maxDistance = maxDistance;

                    EntityManager.AddComponentData(entities[i],contraint);
                }

                float3 dv = new float3( 0.0f, minDistance, 0.0f );
                for (int i = 0; i < count; i++)
                {
                    var position = new Position
                    {
                        Value = center - (dv * (float) i)
                    };
                    EntityManager.SetComponentData(entities[i],position);
                }

                EntityManager.RemoveComponent<SpawnChain>(sourceEntity);

                entities.Dispose();
            }
            spawnInstances.Dispose();
        }
    }
}
