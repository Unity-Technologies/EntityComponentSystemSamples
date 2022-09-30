using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Samples.FixedTimestepSystem
{
    public struct Projectile : IComponentData
    {
        public float SpawnTime;
        public float3 SpawnPos;
    }

    [RequireMatchingQueriesForUpdate]
    public partial class MoveProjectilesSystem : SystemBase
    {
        BeginSimulationEntityCommandBufferSystem m_beginSimEcbSystem;
        protected override void OnCreate()
        {
            m_beginSimEcbSystem = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = m_beginSimEcbSystem.CreateCommandBuffer().AsParallelWriter();
            float timeSinceLoad = (float) SystemAPI.Time.ElapsedTime;
            float projectileSpeed = 5.0f;
            Entities
                .WithName("MoveProjectiles")
#if !ENABLE_TRANSFORM_V1
                .ForEach((Entity projectileEntity, int entityInQueryIndex, ref LocalToWorldTransform transform, in Projectile projectile) =>
#else
                .ForEach((Entity projectileEntity, int entityInQueryIndex, ref Translation translation, in Projectile projectile) =>
#endif
                {
                    float aliveTime = (timeSinceLoad - projectile.SpawnTime);
                    if (aliveTime > 5.0f)
                    {
                        ecb.DestroyEntity(entityInQueryIndex, projectileEntity);
                    }
#if !ENABLE_TRANSFORM_V1
                    transform.Value.Position.x = projectile.SpawnPos.x + aliveTime * projectileSpeed;
#else
                    translation.Value.x = projectile.SpawnPos.x + aliveTime * projectileSpeed;
#endif
                }).ScheduleParallel();
            m_beginSimEcbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
