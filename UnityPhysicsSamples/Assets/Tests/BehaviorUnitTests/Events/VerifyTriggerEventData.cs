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
    [UpdateBefore(typeof(StepPhysicsWorld))]
    public class VerifyTriggerEventDataSystem : SystemBase
    {
        EntityQuery m_VerificationGroup;
        StepPhysicsWorld m_StepPhysicsWorld;

        protected override void OnCreate()
        {
            m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            m_VerificationGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyTriggerEventDataData) }
            });
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

        protected override void OnUpdate()
        {
            SimulationCallbacks.Callback testTriggerEventCallback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
            {
                return new VerifyTriggerEventDataJob
                {
                    Bodies = world.Bodies,
                    VerificationData = GetComponentDataFromEntity<VerifyTriggerEventDataData>(true)
                }.Schedule(simulation, ref world, inDeps);
            };

            m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostSolveJacobians, testTriggerEventCallback, Dependency);
        }
    }
}
