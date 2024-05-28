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
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new VerifyCollisionEventsData());

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
    public partial struct VerifyCollisionEventsSystem : ISystem
    {
        public NativeArray<int> NumEvents;

        public void OnCreate(ref SystemState state)
        {
            NumEvents = new NativeArray<int>(1, Allocator.Persistent);
            state.RequireForUpdate<VerifyCollisionEventsData>();
        }

        public void OnDestroy(ref SystemState state)
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

        public void OnUpdate(ref SystemState state)
        {
            SimulationSingleton simSingleton = SystemAPI.GetSingleton<SimulationSingleton>();

            state.Dependency = new VerifyCollisionEventsJob
            {
                NumEvents = NumEvents,
                VerificationData = SystemAPI.GetComponentLookup<VerifyCollisionEventsData>(true)
            }.Schedule(simSingleton, state.Dependency);

            state.Dependency = new VerifyCollisionEventsPostJob
            {
                NumEvents = NumEvents,
                SimulationType = simSingleton.Type,
                VerificationData = SystemAPI.GetComponentLookup<VerifyCollisionEventsData>(true)
            }.Schedule(state.Dependency);
        }
    }
}
