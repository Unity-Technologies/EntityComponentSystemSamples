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
        public int ExpectedValue;
    }

    [Serializable]
    public class VerifyTriggerEvents : MonoBehaviour, IConvertGameObjectToEntity
    {
        public int ExpectedValue = 4;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new VerifyTriggerEventsData() { ExpectedValue = ExpectedValue });

#if HAVOK_PHYSICS_EXISTS
            Havok.Physics.HavokConfiguration config = Havok.Physics.HavokConfiguration.Default;
            config.EnableSleeping = 0;
            dstManager.AddComponentData(entity, config);
#endif
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(StepPhysicsWorld))]
    public class VerifyTriggerEventsSystem : SystemBase
    {
        EntityQuery m_VerificationGroup;
        StepPhysicsWorld m_StepPhysicsWorld;

        public NativeReference<int> NumEvents;

        protected override void OnCreate()
        {
            m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            m_VerificationGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyTriggerEventsData) }
            });
            NumEvents = new NativeReference<int>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            NumEvents.Dispose();
        }

        struct VerifyTriggerEventsJob : ITriggerEventsJob
        {
            private bool m_Initialized;

            public NativeReference<int> NumEvents;

            [ReadOnly]
            public ComponentDataFromEntity<VerifyTriggerEventsData> VerificationData;

            public void Execute(TriggerEvent triggerEvent)
            {
                if (!m_Initialized)
                {
                    m_Initialized = true;
                    NumEvents.Value = 0;
                }

                NumEvents.Value++;
            }
        }

        struct VerifyTriggerEventsPostJob : IJob
        {
            public NativeReference<int> NumEvents;

            public SimulationType SimulationType;

            [ReadOnly]
            public ComponentDataFromEntity<VerifyTriggerEventsData> VerificationData;

            [DeallocateOnJobCompletion]
            public NativeArray<Entity> Entities;

            public void Execute()
            {
                Assert.AreEqual(NumEvents.Value, VerificationData[Entities[0]].ExpectedValue);
            }
        }

        protected override void OnUpdate()
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
                    VerificationData = GetComponentDataFromEntity<VerifyTriggerEventsData>(true),
                    Entities = m_VerificationGroup.ToEntityArray(Allocator.TempJob)
                }.Schedule(inDeps);
            };

            m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostSolveJacobians, testTriggerEventsCallback, Dependency);
            m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostSolveJacobians, testTriggerEventsPostCallback, Dependency);
        }
    }
}
