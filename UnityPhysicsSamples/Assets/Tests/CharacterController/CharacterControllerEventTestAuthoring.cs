using Unity.Assertions;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Stateful;
using UnityEngine;

public struct CharacterControllerEventTest : IComponentData
{
    public bool IsFirstFrame;
}

public class CharacterControllerEventTestAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new CharacterControllerEventTest
        {
            IsFirstFrame = true
        });
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(TriggerEventConversionSystem))]
[UpdateAfter(typeof(CollisionEventConversionSystem))]
public class CharacterControllerEventTestStstem : SystemBase
{
    private CharacterControllerSystem m_CCSystem;

    protected override void OnCreate()
    {
        m_CCSystem = World.GetOrCreateSystem<CharacterControllerSystem>();
    }

    protected override void OnUpdate()
    {
        Dependency = JobHandle.CombineDependencies(m_CCSystem.OutDependency, Dependency);

        Entities
            .WithName("CCEventsTestJob")
            .WithBurst()
            .ForEach((Entity ccEntity, ref DynamicBuffer<StatefulCollisionEvent> collisionEvents,
                ref DynamicBuffer<StatefulTriggerEvent> triggerEvents, ref CharacterControllerEventTest test) =>
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
                        Assert.IsTrue(details.IsValid == 1);
                        Assert.IsTrue(details.NumberOfContactPoints == 1);
                    }
                    if (triggerEvents.Length > 0)
                    {
                        var triggerEvent = triggerEvents[0];
                        Assert.IsTrue(triggerEvent.EntityA == ccEntity);
                        Assert.IsTrue(triggerEvent.GetOtherEntity(ccEntity) == triggerEvent.EntityB);
                        Assert.IsTrue(triggerEvent.GetOtherEntity(triggerEvent.EntityB) == ccEntity);
                    }
                }).Schedule();
    }
}
