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
    [UpdateAfter(typeof(StepPhysicsWorld))]
    [UpdateBefore(typeof(ExportPhysicsWorld))]
    public partial class VerifyTriggerEventsSystem : SystemBase
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
            RequireForUpdate(m_VerificationGroup);
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

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            this.RegisterPhysicsRuntimeSystemReadOnly();
        }

        protected override void OnUpdate()
        {
            Dependency = new VerifyTriggerEventsJob
            {
                NumEvents = NumEvents,
                VerificationData = GetComponentDataFromEntity<VerifyTriggerEventsData>(true)
            }.Schedule(m_StepPhysicsWorld.Simulation, Dependency);

            Dependency = new VerifyTriggerEventsPostJob
            {
                NumEvents = NumEvents,
                SimulationType = m_StepPhysicsWorld.Simulation.Type,
                VerificationData = GetComponentDataFromEntity<VerifyTriggerEventsData>(true),
                Entities = m_VerificationGroup.ToEntityArray(Allocator.TempJob)
            }.Schedule(Dependency);
        }
    }
}
