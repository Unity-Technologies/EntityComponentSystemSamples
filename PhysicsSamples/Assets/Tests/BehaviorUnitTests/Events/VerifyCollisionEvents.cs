using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public struct VerifyCollisionEventsData : IComponentData
    {
    }

    [Serializable]
    public class VerifyCollisionEvents : MonoBehaviour
    {
        class VerifyCollisionEventsBaker : Baker<VerifyCollisionEvents>
        {
            public override void Bake(VerifyCollisionEvents authoring)
            {
                AddComponent(new VerifyCollisionEventsData());

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
    public partial class VerifyCollisionEventsSystem : SystemBase
    {
        public NativeArray<int> NumEvents;

        protected override void OnCreate()
        {
            NumEvents = new NativeArray<int>(1, Allocator.Persistent);
            RequireForUpdate<VerifyCollisionEventsData>();
        }

        protected override void OnDestroy()
        {
            NumEvents.Dispose();
        }

        struct VerifyCollisionEventsJob : ICollisionEventsJob
        {
            private bool m_Initialized;

            public NativeArray<int> NumEvents;

            [ReadOnly]
            public ComponentLookup<VerifyCollisionEventsData> VerificationData;

            public void Execute(CollisionEvent collisionEvent)
            {
                if (!m_Initialized)
                {
                    m_Initialized = true;
                    NumEvents[0] = 0;
                }

                NumEvents[0]++;
            }
        }

        struct VerifyCollisionEventsPostJob : IJob
        {
            public NativeArray<int> NumEvents;

            public SimulationType SimulationType;

            [ReadOnly]
            public ComponentLookup<VerifyCollisionEventsData> VerificationData;

            public void Execute()
            {
                Assert.AreEqual(NumEvents[0], 4);
            }
        }

        protected override void OnUpdate()
        {
            SimulationSingleton simSingleton = SystemAPI.GetSingleton<SimulationSingleton>();

            Dependency = new VerifyCollisionEventsJob
            {
                NumEvents = NumEvents,
                VerificationData = GetComponentLookup<VerifyCollisionEventsData>(true)
            }.Schedule(simSingleton, Dependency);

            Dependency = new VerifyCollisionEventsPostJob
            {
                NumEvents = NumEvents,
                SimulationType = simSingleton.Type,
                VerificationData = GetComponentLookup<VerifyCollisionEventsData>(true)
            }.Schedule(Dependency);
        }
    }
}
