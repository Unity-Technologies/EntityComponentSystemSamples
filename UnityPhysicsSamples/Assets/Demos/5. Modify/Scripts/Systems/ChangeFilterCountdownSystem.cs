using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

public struct ChangeFilterCountdown : IComponentData
{
    public CollisionFilter Filter;
    public int Countdown;
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(BuildPhysicsWorld))]
public class ChangeFilterCountdownSystem : SystemBase
{
    private BuildPhysicsWorld m_BuildPhysicsWorld;
    private EndFixedStepSimulationEntityCommandBufferSystem m_CommandBufferSystem;

    protected override void OnCreate()
    {
        m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_CommandBufferSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
        RequireForUpdate(GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(ChangeFilterCountdown) }
        }));
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

                collider.Value.Value.Filter = tag.Filter;
                commandBuffer.RemoveComponent<ChangeFilterCountdown>(entity);
            }).Schedule();

        m_BuildPhysicsWorld.AddInputDependency(Dependency);
    }
}
