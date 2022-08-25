using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

[GenerateAuthoringComponent]
public struct VerifyNumEvents : IComponentData {}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(StepPhysicsWorld))]
[UpdateBefore(typeof(EndFramePhysicsSystem))]
public partial class VerifyNumEventsSystem : SystemBase
{
    private StepPhysicsWorld m_StepPhysicsWorld;
    private BuildPhysicsWorld m_BuildPhysicsWorld;

    protected override void OnCreate()
    {
        m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
        m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
        RequireForUpdate(GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(VerifyNumEvents) }
        }));
    }

    [BurstCompile]
    struct GetNumTriggerEvents : ITriggerEventsJob
    {
        public NativeReference<int> NumTriggerEvents;

        public void Execute(TriggerEvent triggerEvent)
        {
            NumTriggerEvents.Value++;
        }
    }

    [BurstCompile]
    struct GetNumCollisionEvents : ICollisionEventsJob
    {
        public NativeReference<int> NumCollisionEvents;

        public void Execute(CollisionEvent collisionEvent)
        {
            NumCollisionEvents.Value++;
        }
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        this.RegisterPhysicsRuntimeSystemReadOnly();
    }

    protected override void OnUpdate()
    {
        Dependency.Complete();

        var numDynamicBodies = m_BuildPhysicsWorld.PhysicsWorld.NumDynamicBodies;

        NativeReference<int> numEvents = new NativeReference<int>(Allocator.Persistent);
        numEvents.Value = 0;

        var getNumTriggerEventsJob = new GetNumTriggerEvents
        {
            NumTriggerEvents = numEvents
        }.Schedule(m_StepPhysicsWorld.Simulation, default);

        var getNumCollisionEventsJob = new GetNumCollisionEvents
        {
            NumCollisionEvents = numEvents
        }.Schedule(m_StepPhysicsWorld.Simulation, getNumTriggerEventsJob);

        getNumCollisionEventsJob.Complete();

        // The test is set up in a way that there is one event for each dynamic body
        Assert.IsTrue(numEvents.Value == numDynamicBodies);

        numEvents.Dispose();
    }
}
