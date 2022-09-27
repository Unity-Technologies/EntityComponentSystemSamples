using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

public struct ChangeFilterCountdown : IComponentData
{
    public CollisionFilter Filter;
    public int Countdown;
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial class ChangeFilterCountdownSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem m_CommandBufferSystem;

    protected override void OnCreate()
    {
        m_CommandBufferSystem = World.GetOrCreateSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
        RequireForUpdate<ChangeFilterCountdown>();
    }

    protected override void OnUpdate()
    {
        var commandBuffer = m_CommandBufferSystem.CreateCommandBuffer();

        // No need to be parallel, as only one entity per explosion cluster
        // shall have ChangeFilterCountdown
        Entities
            .WithName("ChangeFilterCountdown")
            .WithBurst()
            .ForEach((int entityInQueryIndex, ref Entity entity, ref PhysicsCollider collider, ref ChangeFilterCountdown tag) =>
            {
                if (--tag.Countdown > 0) return;

                collider.Value.Value.SetCollisionFilter(tag.Filter);
                commandBuffer.RemoveComponent<ChangeFilterCountdown>(entity);
            }).Schedule();
    }
}
