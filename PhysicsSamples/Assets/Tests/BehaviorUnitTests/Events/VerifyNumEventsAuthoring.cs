using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

public struct VerifyNumEvents : IComponentData {}

[UnityEngine.DisallowMultipleComponent]
public class VerifyNumEventsAuthoring : UnityEngine.MonoBehaviour
{
    class VerifyNumEventsBaker : Baker<VerifyNumEventsAuthoring>
    {
        public override void Bake(VerifyNumEventsAuthoring authoring)
        {
            AddComponent<VerifyNumEvents>();
        }
    }
}

[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateAfter(typeof(PhysicsSimulationGroup))]
public partial class VerifyNumEventsSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<VerifyNumEvents>();
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

    protected override void OnUpdate()
    {
        Dependency.Complete();

        PhysicsWorld world = GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        var numDynamicBodies = world.NumDynamicBodies;

        NativeReference<int> numEvents = new NativeReference<int>(Allocator.Persistent);
        numEvents.Value = 0;

        SimulationSingleton simSingleton = GetSingleton<SimulationSingleton>();

        var getNumTriggerEventsJob = new GetNumTriggerEvents
        {
            NumTriggerEvents = numEvents
        }.Schedule(simSingleton, default);

        var getNumCollisionEventsJob = new GetNumCollisionEvents
        {
            NumCollisionEvents = numEvents
        }.Schedule(simSingleton, getNumTriggerEventsJob);

        getNumCollisionEventsJob.Complete();

        // The test is set up in a way that there is one event for each dynamic body
        Assert.IsTrue(numEvents.Value == numDynamicBodies);

        numEvents.Dispose();
    }
}
