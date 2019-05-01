using System;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Samples.FixedTimestepSystem
{
    public struct FixedRateSpawner : IComponentData
    {
        public Entity Prefab;
    }

    [DisableAutoCreation]
    public class FixedRateSpawnerSystem : ComponentSystem
    {
        EntityQuery m_MainGroup;
        protected override void OnCreateManager()
        {
            m_MainGroup = GetEntityQuery(
                ComponentType.ReadOnly<FixedRateSpawner>(),
                ComponentType.ReadOnly<Translation>());
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((Entity spawnerEntity, ref FixedRateSpawner spawnerData, ref Translation translation) =>
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
