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
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<VerifyTriggerEventDataData>(entity);

#if HAVOK_PHYSICS_EXISTS
                Havok.Physics.HavokConfiguration config = Havok.Physics.HavokConfiguration.Default;
                config.EnableSleeping = 0;
                AddComponent(entity, config);
#endif
            }
        }
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    [UpdateBefore(typeof(ExportPhysicsWorld))]
    public partial struct VerifyTriggerEventDataSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VerifyTriggerEventDataData>();
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

        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new VerifyTriggerEventDataJob
            {
                Bodies = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.Bodies,
                VerificationData = SystemAPI.GetComponentLookup<VerifyTriggerEventDataData>(true)
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
        }
    }
}
