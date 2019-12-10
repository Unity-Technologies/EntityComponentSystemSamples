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
        protected override void OnUpdate()
        {
            Entities.ForEach((Entity spawnerEntity, ref VariableRateSpawner spawnerData, ref Translation translation) =>
            {
                var spawnTime = (float)Time.ElapsedTime;
                var newEntity = PostUpdateCommands.Instantiate(spawnerData.Prefab);
                PostUpdateCommands.AddComponent(newEntity, new Parent {Value = spawnerEntity});
                PostUpdateCommands.AddComponent(newEntity, new LocalToParent());
                PostUpdateCommands.SetComponent(newEntity, new Translation {Value = new float3(0, 0.3f * math.sin(5.0f * spawnTime), 0)});
                PostUpdateCommands.SetComponent(newEntity, new ProjectileSpawnTime{SpawnTime = spawnTime});
            });
        }
    }

}
