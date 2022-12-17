using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

public struct ChangeFilterCountdown : IComponentData
{
    public CollisionFilter Filter;
    public int Countdown;
}

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial struct ChangeFilterCountdownSystem : ISystem
{
    [BurstCompile]
    private partial struct ChangeFilterCountDownJob : IJobEntity
    {
        public EntityCommandBuffer CommandBuffer;
        private void Execute(Entity entity, ref PhysicsCollider collider, ref ChangeFilterCountdown tag)
        {
            if (--tag.Countdown > 0) return;

            collider.Value.Value.SetCollisionFilter(tag.Filter);
            CommandBuffer.RemoveComponent<ChangeFilterCountdown>(entity);
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ChangeFilterCountdown>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var commandBuffer = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        state.Dependency = new ChangeFilterCountDownJob
        {
            CommandBuffer = commandBuffer
        }.Schedule(state.Dependency);
    }
}
