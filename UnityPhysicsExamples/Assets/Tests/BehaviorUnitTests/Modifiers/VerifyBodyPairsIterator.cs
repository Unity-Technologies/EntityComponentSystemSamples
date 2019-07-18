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

    [UpdateBefore(typeof(StepPhysicsWorld))]
    public class VerifyBodyPairsIteratorSystem : JobComponentSystem
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
            public NativeSlice<RigidBody> Bodies;

            [ReadOnly]
            public ComponentDataFromEntity<VerifyBodyPairsIteratorData> VerificationData;

            public void Execute(ref ModifiableBodyPair pair)
            {
                Assert.AreNotEqual(pair.BodyIndices.BodyAIndex, pair.BodyIndices.BodyBIndex);
                Assert.AreEqual(pair.Entities.EntityA, Bodies[pair.BodyIndices.BodyAIndex].Entity);
                Assert.AreEqual(pair.Entities.EntityB, Bodies[pair.BodyIndices.BodyBIndex].Entity);
                Assert.AreEqual(pair.Entities.EntityA.Version, 1);
                Assert.AreEqual(pair.Entities.EntityB.Version, 1);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            SimulationCallbacks.Callback verifyBodyPairsIteratorJobCallback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
            {
                return new VerifyBodyPairsIteratorJob
                {
                    Bodies = world.Bodies,
                    VerificationData = GetComponentDataFromEntity<VerifyBodyPairsIteratorData>(true)
                }.Schedule(simulation, ref world, inDeps);
            };

            m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateDispatchPairs, verifyBodyPairsIteratorJobCallback, inputDeps);

            return inputDeps;
        }
    }
}
