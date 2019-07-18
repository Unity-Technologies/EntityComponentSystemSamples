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
        }
    }

    [UpdateBefore(typeof(StepPhysicsWorld))]
    public class VerifyCollisionEventsSystem : JobComponentSystem
    {
        EntityQuery m_VerificationGroup;
        StepPhysicsWorld m_StepPhysicsWorld;

        public NativeArray<int> NumEvents;

        protected override void OnCreate()
        {
            m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            m_VerificationGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyCollisionEventsData) }
            });
            NumEvents = new NativeArray<int>(1, Allocator.Persistent);
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

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            SimulationCallbacks.Callback testCollisionEventsCallback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
            {
                return new VerifyCollisionEventsJob
                {
                    NumEvents = NumEvents,
                    VerificationData = GetComponentDataFromEntity<VerifyCollisionEventsData>(true)
                }.Schedule(simulation, ref world, inDeps);
            };

            SimulationCallbacks.Callback testCollisionEventsPostCallback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
            {
                return new VerifyCollisionEventsPostJob
                {
                    NumEvents = NumEvents,
                    SimulationType = simulation.Type,
                    VerificationData = GetComponentDataFromEntity<VerifyCollisionEventsData>(true)
                }.Schedule(inDeps);
            };

            m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostSolveJacobians, testCollisionEventsCallback, inputDeps);
            m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostSolveJacobians, testCollisionEventsPostCallback, inputDeps);

            return inputDeps;
        }
    }
}
