using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public struct VerifyTriggerEventDataData : IComponentData
    {
    }

    [Serializable]
    public class VerifyTriggerEventData : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new VerifyTriggerEventDataData());

#if HAVOK_PHYSICS_EXISTS
            Havok.Physics.HavokConfiguration config = Havok.Physics.HavokConfiguration.Default;
            config.EnableSleeping = 0;
            dstManager.AddComponentData(entity, config);
#endif
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(StepPhysicsWorld))]
    [UpdateBefore(typeof(ExportPhysicsWorld))]
    public partial class VerifyTriggerEventDataSystem : SystemBase
    {
        BuildPhysicsWorld m_BuildPhysicsWorld;
        StepPhysicsWorld m_StepPhysicsWorld;

        protected override void OnCreate()
        {
            m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            RequireForUpdate(GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyTriggerEventDataData) }
            }));
        }

        struct VerifyTriggerEventDataJob : ITriggerEventsJob
        {
            [ReadOnly]
            public NativeArray<RigidBody> Bodies;

            [ReadOnly]
            public ComponentDataFromEntity<VerifyTriggerEventDataData> VerificationData;

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

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            this.RegisterPhysicsRuntimeSystemReadOnly();
        }

        protected override void OnUpdate()
        {
            Dependency = new VerifyTriggerEventDataJob
            {
                Bodies = m_BuildPhysicsWorld.PhysicsWorld.Bodies,
                VerificationData = GetComponentDataFromEntity<VerifyTriggerEventDataData>(true)
            }.Schedule(m_StepPhysicsWorld.Simulation, Dependency);
        }
    }
}
