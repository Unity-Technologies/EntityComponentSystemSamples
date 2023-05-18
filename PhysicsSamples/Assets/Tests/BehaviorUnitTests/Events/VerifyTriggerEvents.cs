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
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new VerifyTriggerEventsData() { ExpectedValue = authoring.ExpectedValue });

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
    public partial struct VerifyTriggerEventsSystem : ISystem
    {
        EntityQuery m_VerificationGroup;
        public NativeReference<int> NumEvents;

        public void OnCreate(ref SystemState state)
        {
            m_VerificationGroup = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyTriggerEventsData) }
            });
            NumEvents = new NativeReference<int>(Allocator.Persistent);
            state.RequireForUpdate(m_VerificationGroup);
        }

        public void OnDestroy(ref SystemState state)
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

        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new VerifyTriggerEventsJob
            {
                NumEvents = NumEvents,
                VerificationData = SystemAPI.GetComponentLookup<VerifyTriggerEventsData>(true)
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);

            state.Dependency = new VerifyTriggerEventsPostJob
            {
                NumEvents = NumEvents,
                VerificationData = SystemAPI.GetComponentLookup<VerifyTriggerEventsData>(true),
                Entities = m_VerificationGroup.ToEntityArray(Allocator.TempJob)
            }.Schedule(state.Dependency);
        }
    }
}
