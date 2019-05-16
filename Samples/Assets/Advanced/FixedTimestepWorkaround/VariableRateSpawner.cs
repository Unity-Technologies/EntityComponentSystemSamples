using System;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Samples.FixedTimestepSystem
{
    public struct VariableRateSpawner : IComponentData
    {
        public Entity Prefab;
    }

    public class VariableRateSpawnerSystem : ComponentSystem
    {
        EntityQuery m_MainGroup;
        protected override void OnCreateManager()
        {
            m_MainGroup = GetEntityQuery(
                ComponentType.ReadOnly<VariableRateSpawner>(),
                ComponentType.ReadOnly<Translation>());
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((Entity spawnerEntity, ref VariableRateSpawner spawnerData, ref Translation translation) =>
            {
                var spawnTime = Time.timeSinceLevelLoad;
                var newEntity = PostUpdateCommands.Instantiate(spawnerData.Prefab);
                PostUpdateCommands.AddComponent(newEntity, new Parent {Value = spawnerEntity});
                PostUpdateCommands.AddComponent(newEntity, new LocalToParent());
                PostUpdateCommands.SetComponent(newEntity, new Translation {Value = new float3(0, 0.3f * math.sin(5.0f * spawnTime), 0)});
                PostUpdateCommands.SetComponent(newEntity, new ProjectileSpawnTime{SpawnTime = spawnTime});
            });
        }
    }

}
