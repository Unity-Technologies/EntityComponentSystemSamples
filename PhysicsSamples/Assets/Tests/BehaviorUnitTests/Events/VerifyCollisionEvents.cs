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
    public class VerifyCollisionEvents : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new VerifyCollisionEventsData());

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
    public partial class VerifyCollisionEventsSystem : SystemBase
    {
        StepPhysicsWorld m_StepPhysicsWorld;

        public NativeArray<int> NumEvents;

        protected override void OnCreate()
        {
            m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            NumEvents = new NativeArray<int>(1, Allocator.Persistent);
            RequireForUpdate(GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyCollisionEventsData) }
            }));
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
            public ComponentDataFromEntity<VerifyCollisionEventsData> VerificationData;

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
            public ComponentDataFromEntity<VerifyCollisionEventsData> VerificationData;

            public void Execute()
            {
                Assert.AreEqual(NumEvents[0], 4);
            }
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            this.RegisterPhysicsRuntimeSystemReadOnly();
        }

        protected override void OnUpdate()
        {
            Dependency = new VerifyCollisionEventsJob
            {
                NumEvents = NumEvents,
                VerificationData = GetComponentDataFromEntity<VerifyCollisionEventsData>(true)
            }.Schedule(m_StepPhysicsWorld.Simulation, Dependency);

            Dependency = new VerifyCollisionEventsPostJob
            {
                NumEvents = NumEvents,
                SimulationType = m_StepPhysicsWorld.Simulation.Type,
                VerificationData = GetComponentDataFromEntity<VerifyCollisionEventsData>(true)
            }.Schedule(Dependency);
        }
    }
}
