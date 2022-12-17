using Unity.Assertions;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Stateful;
using UnityEngine;

public struct CharacterControllerEventTest : IComponentData
{
    public bool IsFirstFrame;
}

public class CharacterControllerEventTestAuthoring : MonoBehaviour
{
    class CharacterControllerEventTestBaker : Baker<CharacterControllerEventTestAuthoring>
    {
        public override void Bake(CharacterControllerEventTestAuthoring authoring)
        {
            AddComponent(new CharacterControllerEventTest { IsFirstFrame = true });
        }
    }
}

[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(CharacterControllerSystem))]
public partial struct CharacterControllerEventTestSystem : ISystem
{
    [BurstCompile]
    public partial struct IJobEntity_CharacterControllerEventTest : IJobEntity
    {
        private void Execute(Entity ccEntity, ref DynamicBuffer<StatefulCollisionEvent> collisionEvents,
            ref DynamicBuffer<StatefulTriggerEvent> triggerEvents, ref CharacterControllerEventTest test)
        {
            Assert.IsTrue(collisionEvents.Length <= 1);
            Assert.IsTrue(triggerEvents.Length <= 1);

            if (collisionEvents.Length == 0 || triggerEvents.Length == 0)
            {
                if (!test.IsFirstFrame)
                {
                    Assert.IsTrue(triggerEvents.Length != 0, "No TriggerEvents registered!");
                    Assert.IsTrue(collisionEvents.Length != 0, "No CollisionEvents registered!");
                }
                test.IsFirstFrame = false;
            }

            if (collisionEvents.Length > 0)
            {
                var collisionEvent = collisionEvents[0];
                Assert.IsTrue(collisionEvent.EntityA == ccEntity);
                Assert.IsTrue(collisionEvent.GetOtherEntity(ccEntity) == collisionEvent.EntityB);
                Assert.IsTrue(collisionEvent.GetOtherEntity(collisionEvent.EntityB) == ccEntity);
                Assert.IsTrue(collisionEvent.Normal.Equals(math.up()));
                Assert.IsTrue(collisionEvent.GetNormalFrom(collisionEvent.EntityB).Equals(math.up()));
                Assert.IsTrue(collisionEvent.GetNormalFrom(ccEntity).Equals(-math.up()));
                Assert.IsTrue(collisionEvent.TryGetDetails(out StatefulCollisionEvent.Details details));
                Assert.IsTrue(details.IsValid);
                Assert.IsTrue(details.NumberOfContactPoints == 1);
            }
            if (triggerEvents.Length > 0)
            {
                var triggerEvent = triggerEvents[0];
                Assert.IsTrue(triggerEvent.EntityA == ccEntity);
                Assert.IsTrue(triggerEvent.GetOtherEntity(ccEntity) == triggerEvent.EntityB);
                Assert.IsTrue(triggerEvent.GetOtherEntity(triggerEvent.EntityB) == ccEntity);
            }
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new IJobEntity_CharacterControllerEventTest().Schedule(state.Dependency);
    }
}
