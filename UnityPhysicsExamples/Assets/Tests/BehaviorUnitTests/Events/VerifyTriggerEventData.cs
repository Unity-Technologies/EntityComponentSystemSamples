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
        }
    }

    [UpdateBefore(typeof(StepPhysicsWorld))]
    public class VerifyTriggerEventDataSystem : JobComponentSystem
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
            public NativeSlice<RigidBody> Bodies;

            [ReadOnly]
            public ComponentDataFromEntity<VerifyTriggerEventDataData> VerificationData;

            public void Execute(TriggerEvent triggerEvent)
            {
                // Trigger event is between a static and dynamic box.
                // Verify all data in the provided event struct.
                Assert.AreNotEqual(triggerEvent.BodyIndices.BodyAIndex, triggerEvent.BodyIndices.BodyBIndex);
                Assert.AreEqual(triggerEvent.ColliderKeys.ColliderKeyA.Value, ColliderKey.Empty.Value);
                Assert.AreEqual(triggerEvent.ColliderKeys.ColliderKeyB.Value, ColliderKey.Empty.Value);
                Assert.AreEqual(triggerEvent.Entities.EntityA, Bodies[triggerEvent.BodyIndices.BodyAIndex].Entity);
                Assert.AreEqual(triggerEvent.Entities.EntityB, Bodies[triggerEvent.BodyIndices.BodyBIndex].Entity);
                Assert.AreEqual(triggerEvent.Entities.EntityA.Version, 1);
                Assert.AreEqual(triggerEvent.Entities.EntityB.Version, 1);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            SimulationCallbacks.Callback testTriggerEventCallback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
            {
                return new VerifyTriggerEventDataJob
                {
                    Bodies = world.Bodies,
                    VerificationData = GetComponentDataFromEntity<VerifyTriggerEventDataData>(true)
                }.Schedule(simulation, ref world, inDeps);
            };

            m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostSolveJacobians, testTriggerEventCallback, inputDeps);

            return inputDeps;
        }
    }
}
