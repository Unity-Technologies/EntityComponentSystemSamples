using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public struct VerifyTriggerEventDataData : IComponentData
    {
    }

    [Serializable]
    public class VerifyTriggerEventData : MonoBehaviour
    {
        class VerifyTriggerEventDataBaker : Baker<VerifyTriggerEventData>
        {
            public override void Bake(VerifyTriggerEventData authoring)
            {
                AddComponent<VerifyTriggerEventDataData>();

#if HAVOK_PHYSICS_EXISTS
                Havok.Physics.HavokConfiguration config = Havok.Physics.HavokConfiguration.Default;
                config.EnableSleeping = 0;
                AddComponent(config);
#endif
            }
        }
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    [UpdateBefore(typeof(ExportPhysicsWorld))]
    public partial class VerifyTriggerEventDataSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<VerifyTriggerEventDataData>();
        }

        struct VerifyTriggerEventDataJob : ITriggerEventsJob
        {
            [ReadOnly]
            public NativeArray<RigidBody> Bodies;

            [ReadOnly]
            public ComponentLookup<VerifyTriggerEventDataData> VerificationData;

            public void Execute(TriggerEvent triggerEvent)
            {
                // Trigger event is between a static and dynamic box.
                // Verify all data in the provided event struct.
                Assert.AreNotEqual(triggerEvent.BodyIndexA, triggerEvent.BodyIndexB);
                Assert.AreEqual(triggerEvent.ColliderKeyA.Value, ColliderKey.Empty.Value);
                Assert.AreEqual(triggerEvent.ColliderKeyB.Value, ColliderKey.Empty.Value);
                Assert.AreEqual(triggerEvent.EntityA, Bodies[triggerEvent.BodyIndexA].Entity);
                Assert.AreEqual(triggerEvent.EntityB, Bodies[triggerEvent.BodyIndexB].Entity);
            }
        }

        protected override void OnUpdate()
        {
            Dependency = new VerifyTriggerEventDataJob
            {
                Bodies = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.Bodies,
                VerificationData = GetComponentLookup<VerifyTriggerEventDataData>(true)
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);
        }
    }
}
