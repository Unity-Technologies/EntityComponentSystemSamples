using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Samples.FixedTimestepSystem
{
    public struct VariableRateSpawner : IComponentData
    {
        public Entity Prefab;
        public float3 SpawnPos;
    }

    [RequireMatchingQueriesForUpdate]
    public partial class VariableRateSpawnerSystem : SystemBase
    {
        private BeginSimulationEntityCommandBufferSystem ecbSystem;
        protected override void OnCreate()
        {
            ecbSystem = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            float spawnTime = (float)SystemAPI.Time.ElapsedTime;
            var ecb = ecbSystem.CreateCommandBuffer();
            Entities
                .WithName("VariableRateSpawner")
                .ForEach((in VariableRateSpawner spawner) =>
                {
                    var projectileEntity = ecb.Instantiate(spawner.Prefab);
                    var spawnPos = spawner.SpawnPos;
                    spawnPos.y += 0.3f * math.sin(5.0f * spawnTime);
#if !ENABLE_TRANSFORM_V1
                    ecb.SetComponent(projectileEntity, new LocalToWorldTransform {Value = UniformScaleTransform.FromPosition(spawnPos)});
#else
                    ecb.SetComponent(projectileEntity, new Translation {Value = spawnPos});
#endif
                    ecb.SetComponent(projectileEntity, new Projectile
                    {
                        SpawnTime = spawnTime,
                        SpawnPos = spawnPos,
                    });
                }).Schedule();
            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
