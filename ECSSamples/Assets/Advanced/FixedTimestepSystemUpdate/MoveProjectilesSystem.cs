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
    public partial class MoveProjectilesSystem : SystemBase
    {
        BeginSimulationEntityCommandBufferSystem m_beginSimEcbSystem;
        protected override void OnCreate()
        {
            m_beginSimEcbSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = m_beginSimEcbSystem.CreateCommandBuffer().AsParallelWriter();
            float timeSinceLoad = (float) Time.ElapsedTime;
            float projectileSpeed = 5.0f;
            Entities
                .WithName("MoveProjectiles")
                .ForEach((Entity projectileEntity, int entityInQueryIndex, ref Translation translation, in Projectile projectile) =>
                {
                    float aliveTime = (timeSinceLoad - projectile.SpawnTime);
                    if (aliveTime > 5.0f)
                    {
                        ecb.DestroyEntity(entityInQueryIndex, projectileEntity);
                    }
                    translation.Value.x = projectile.SpawnPos.x + aliveTime * projectileSpeed;
                }).ScheduleParallel();
            m_beginSimEcbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
