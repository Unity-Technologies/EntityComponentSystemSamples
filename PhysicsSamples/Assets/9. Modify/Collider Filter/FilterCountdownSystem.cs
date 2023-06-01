using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Modify
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial struct FilterCountdownSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FilterCountdown>();
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new ChangeFilterCountDownJob
            {
                ECB = ecb
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        private partial struct ChangeFilterCountDownJob : IJobEntity
        {
            public EntityCommandBuffer ECB;

            private void Execute(Entity entity, ref PhysicsCollider collider, ref FilterCountdown tag)
            {
                if (--tag.Countdown > 0) return;

                collider.Value.Value.SetCollisionFilter(tag.Filter);
                ECB.RemoveComponent<FilterCountdown>(entity);
            }
        }
    }

    public struct FilterCountdown : IComponentData
    {
        public CollisionFilter Filter;
        public int Countdown;
    }
}
