using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

public struct TriggerEventChecker : IComponentData
{
    public int NumExpectedEvents;
}

[UnityEngine.DisallowMultipleComponent]
public class TriggerEventCheckerAuthoring : UnityEngine.MonoBehaviour
{
    [RegisterBinding(typeof(TriggerEventChecker), "NumExpectedEvents")]
    public int NumExpectedEvents;

    class TriggerEventCheckerBaker : Baker<TriggerEventCheckerAuthoring>
    {
        public override void Bake(TriggerEventCheckerAuthoring authoring)
        {
            TriggerEventChecker component = default(TriggerEventChecker);
            component.NumExpectedEvents = authoring.NumExpectedEvents;
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}

[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateAfter(typeof(PhysicsSimulationGroup))]
public partial struct TriggerEventCheckerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(state.GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(TriggerEventChecker) }
        }));
    }

    public struct CollectTriggerEventsJob : ITriggerEventsJob
    {
        public NativeList<TriggerEvent> m_TriggerEvents;

        public void Execute(TriggerEvent triggerEvent)
        {
            m_TriggerEvents.Add(triggerEvent);
        }
    }

    public partial struct CheckTriggerEventsJob : IJobEntity
    {
        public NativeReference<int> ExpectedNumberOfTriggerEvents;
        public NativeList<TriggerEvent> TriggerEvents;
        [ReadOnly] public PhysicsWorld World;

        public void Execute(Entity entity, ref TriggerEventChecker triggerEventChecker)
        {
            int numTriggerEvents = 0;
            TriggerEvent triggerEvent = default;
            ExpectedNumberOfTriggerEvents.Value += triggerEventChecker.NumExpectedEvents;

            for (int i = 0; i < TriggerEvents.Length; i++)
            {
                if (TriggerEvents[i].EntityA == entity || TriggerEvents[i].EntityB == entity)
                {
                    triggerEvent = TriggerEvents[i];
                    numTriggerEvents++;
                }
            }

            Assert.IsTrue(numTriggerEvents == triggerEventChecker.NumExpectedEvents, "Missing events!");

            if (numTriggerEvents == 0)
            {
                return;
            }

            // Even if component.NumExpectedEvents is > 1, we still take one trigger event, and not all, because the only
            // difference will be in ColliderKeys which we're not checking here
            int nonTriggerBodyIndex = triggerEvent.EntityA == entity ? triggerEvent.BodyIndexA : triggerEvent.BodyIndexB;
            int triggerBodyIndex = triggerEvent.EntityA == entity ? triggerEvent.BodyIndexB : triggerEvent.BodyIndexA;

            Assert.IsTrue(nonTriggerBodyIndex == World.GetRigidBodyIndex(entity), "Wrong body index!");

            RigidBody nonTriggerBody = World.Bodies[nonTriggerBodyIndex];
            RigidBody triggerBody = World.Bodies[triggerBodyIndex];

            bool isTrigger = false;
            unsafe
            {
                ConvexCollider* colliderPtr = (ConvexCollider*)triggerBody.Collider.GetUnsafePtr();
                var material = colliderPtr->Material;

                isTrigger = colliderPtr->Material.CollisionResponse == CollisionResponsePolicy.RaiseTriggerEvents;
            }

            Assert.IsTrue(isTrigger, "Event doesn't have valid trigger index");

            float distance = math.distance(triggerBody.WorldFromBody.pos, nonTriggerBody.WorldFromBody.pos);

            Assert.IsTrue(distance < 10.0f, "The trigger index is wrong!");
        }
    }

    public void OnUpdate(ref SystemState state)
    {
        //Complete the simulation
        state.Dependency.Complete();

        NativeList<TriggerEvent> triggerEvents = new NativeList<TriggerEvent>(Allocator.TempJob);

        var collectTriggerEventsJob = new CollectTriggerEventsJob
        {
            m_TriggerEvents = triggerEvents
        };

        // Collect all events
        var handle = collectTriggerEventsJob.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
        handle.Complete();

        NativeReference<int> expectedNumberOfTriggerEvents = new NativeReference<int>(0, Allocator.TempJob);

        new CheckTriggerEventsJob
        {
            ExpectedNumberOfTriggerEvents = expectedNumberOfTriggerEvents,
            TriggerEvents = triggerEvents,
            World = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld
        }.Run();

        Assert.IsTrue(expectedNumberOfTriggerEvents.Value == triggerEvents.Length, "Incorrect number of events: Expected: " + expectedNumberOfTriggerEvents.Value + " Actual: " + triggerEvents.Length);

        expectedNumberOfTriggerEvents.Dispose();
        triggerEvents.Dispose();
    }
}
