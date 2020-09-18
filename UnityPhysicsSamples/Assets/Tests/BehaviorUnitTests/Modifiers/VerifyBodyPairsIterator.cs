using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public struct VerifyBodyPairsIteratorData : IComponentData
    {
    }

    [Serializable]
    public class VerifyBodyPairsIterator : MonoBehaviour, IConvertGameObjectToEntity
    {
        void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new VerifyBodyPairsIteratorData());
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(StepPhysicsWorld))]
    public class VerifyBodyPairsIteratorSystem : SystemBase
    {
        EntityQuery m_VerificationGroup;
        StepPhysicsWorld m_StepPhysicsWorld;

        protected override void OnCreate()
        {
            m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            m_VerificationGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyBodyPairsIteratorData) }
            });
        }

        struct VerifyBodyPairsIteratorJob : IBodyPairsJob
        {
            [ReadOnly]
            public NativeArray<RigidBody> Bodies;

            [ReadOnly]
            public ComponentDataFromEntity<VerifyBodyPairsIteratorData> VerificationData;

            public void Execute(ref ModifiableBodyPair pair)
            {
                Assert.AreNotEqual(pair.BodyIndexA, pair.BodyIndexB);
                Assert.AreEqual(pair.EntityA, Bodies[pair.BodyIndexA].Entity);
                Assert.AreEqual(pair.EntityB, Bodies[pair.BodyIndexB].Entity);
            }
        }

        protected override void OnUpdate()
        {
            SimulationCallbacks.Callback verifyBodyPairsIteratorJobCallback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
            {
                return new VerifyBodyPairsIteratorJob
                {
                    Bodies = world.Bodies,
                    VerificationData = GetComponentDataFromEntity<VerifyBodyPairsIteratorData>(true)
                }.Schedule(simulation, ref world, inDeps);
            };

            m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateDispatchPairs, verifyBodyPairsIteratorJobCallback, Dependency);
        }
    }
}
