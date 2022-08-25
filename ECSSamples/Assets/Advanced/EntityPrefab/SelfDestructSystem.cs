using Unity.Entities;

public struct SelfDestruct : IComponentData
{
    public float TimeToLive;
}

public partial class SelfDestructSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem m_EndSimECBSystem;

    protected override void OnCreate()
    {
        m_EndSimECBSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        var ecb = m_EndSimECBSystem.CreateCommandBuffer().AsParallelWriter();
        var dt = Time.DeltaTime;
        Entities.ForEach((Entity entity, int entityInQueryIndex, ref SelfDestruct spawner) =>
        {
            if((spawner.TimeToLive -= dt) < 0)
                ecb.DestroyEntity(entityInQueryIndex, entity);
        }).ScheduleParallel();
        m_EndSimECBSystem.AddJobHandleForProducer(Dependency);
    }
}
