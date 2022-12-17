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
    public class VerifyTriggerEvents : MonoBehaviour
    {
        public int ExpectedValue = 4;

        class VerifyTriggerEventsBaker : Baker<VerifyTriggerEvents>
        {
            public override void Bake(VerifyTriggerEvents authoring)
            {
                AddComponent(new VerifyTriggerEventsData() { ExpectedValue = authoring.ExpectedValue });

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
    public partial class VerifyTriggerEventsSystem : SystemBase
    {
        EntityQuery m_VerificationGroup;
        public NativeReference<int> NumEvents;

        protected override void OnCreate()
        {
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
            public ComponentLookup<VerifyTriggerEventsData> VerificationData;

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

            [ReadOnly]
            public ComponentLookup<VerifyTriggerEventsData> VerificationData;

            [DeallocateOnJobCompletion]
            public NativeArray<Entity> Entities;

            public void Execute()
            {
                Assert.AreEqual(NumEvents.Value, VerificationData[Entities[0]].ExpectedValue);
            }
        }

        protected override void OnUpdate()
        {
            Dependency = new VerifyTriggerEventsJob
            {
                NumEvents = NumEvents,
                VerificationData = GetComponentLookup<VerifyTriggerEventsData>(true)
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);

            Dependency = new VerifyTriggerEventsPostJob
            {
                NumEvents = NumEvents,
                VerificationData = GetComponentLookup<VerifyTriggerEventsData>(true),
                Entities = m_VerificationGroup.ToEntityArray(Allocator.TempJob)
            }.Schedule(Dependency);
        }
    }
}
