using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public struct VerifyTriggerEventsData : IComponentData
    {

    }

    [Serializable]
    public class VerifyTriggerEvents : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new VerifyTriggerEventsData());
        }
    }

    [UpdateBefore(typeof(StepPhysicsWorld))]
    public class VerifyTriggerEventsSystem : JobComponentSystem
    {
        EntityQuery m_VerificationGroup;
        StepPhysicsWorld m_StepPhysicsWorld;

        public NativeArray<int> NumEvents;

        protected override void OnCreate()
        {
            m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            m_VerificationGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyTriggerEventsData) }
            });
            NumEvents = new NativeArray<int>(1, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            NumEvents.Dispose();
        }

        struct VerifyTriggerEventsJob : ITriggerEventsJob
        {
            private bool m_Initialized;

            public NativeArray<int> NumEvents;

            [ReadOnly]
            public ComponentDataFromEntity<VerifyTriggerEventsData> VerificationData;

            public void Execute(TriggerEvent triggerEvent)
            {
                if (!m_Initialized)
                {
                    m_Initialized = true;
                    NumEvents[0] = 0;
                }

                NumEvents[0]++;
            }
        }

        struct VerifyTriggerEventsPostJob : IJob
        {
            public NativeArray<int> NumEvents;

            public SimulationType SimulationType;

            [ReadOnly]
            public ComponentDataFromEntity<VerifyTriggerEventsData> VerificationData;

            public void Execute()
            {
                Assert.AreEqual(NumEvents[0], 4);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            SimulationCallbacks.Callback testTriggerEventsCallback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
            {
                return new VerifyTriggerEventsJob
                {
                    NumEvents = NumEvents,
                    VerificationData = GetComponentDataFromEntity<VerifyTriggerEventsData>(true)
                }.Schedule(simulation, ref world, inDeps);
            };

            SimulationCallbacks.Callback testTriggerEventsPostCallback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
            {
                return new VerifyTriggerEventsPostJob
                {
                    NumEvents = NumEvents,
                    SimulationType = simulation.Type,
                    VerificationData = GetComponentDataFromEntity<VerifyTriggerEventsData>(true)
                }.Schedule(inDeps);
            };

            m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostSolveJacobians, testTriggerEventsCallback, inputDeps);
            m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostSolveJacobians, testTriggerEventsPostCallback, inputDeps);

            return inputDeps;
        }
    }
}
