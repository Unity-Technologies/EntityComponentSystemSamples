using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public struct VerifyJacobiansIteratorData : IComponentData
    {

    }

    [Serializable]
    public class VerifyJacobiansIterator : MonoBehaviour, IConvertGameObjectToEntity
    {
        void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new VerifyJacobiansIteratorData());
        }
    }

    [UpdateBefore(typeof(StepPhysicsWorld))]
    public class VerifyJacobiansIteratorSystem : JobComponentSystem
    {
        EntityQuery m_VerificationGroup;
        StepPhysicsWorld m_StepPhysicsWorld;

        protected override void OnCreate()
        {
            m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            m_VerificationGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyJacobiansIteratorData) }
            });
        }

        struct VerifyJacobiansIteratorJob : IJacobiansJob
        {
            [ReadOnly]
            public NativeSlice<RigidBody> Bodies;

            [ReadOnly]
            public ComponentDataFromEntity<VerifyJacobiansIteratorData> VerificationData;

            public void Execute(ref ModifiableJacobianHeader header, ref ModifiableContactJacobian jacobian)
            {
                // Header verification
                Assert.IsFalse(header.AngularChanged);
                Assert.AreNotEqual(header.BodyPair.BodyAIndex, header.BodyPair.BodyBIndex);
                Assert.AreEqual(header.ColliderKeys.ColliderKeyA.Value, ColliderKey.Empty.Value);
                Assert.AreEqual(header.ColliderKeys.ColliderKeyB.Value, ColliderKey.Empty.Value);
                Assert.AreEqual(header.Entities.EntityA, Bodies[header.BodyPair.BodyAIndex].Entity);
                Assert.AreEqual(header.Entities.EntityB, Bodies[header.BodyPair.BodyBIndex].Entity);
                Assert.AreEqual(header.Entities.EntityA.Version, 1);
                Assert.AreEqual(header.Entities.EntityB.Version, 1);
                Assert.AreEqual(header.Flags, (JacobianFlags)0);
                Assert.IsFalse(header.HasColliderKeys);
                Assert.IsFalse(header.HasMassFactors);
                Assert.IsFalse(header.HasSurfaceVelocity);
                Assert.IsFalse(header.ModifiersChanged);
                Assert.AreEqual(header.Type, JacobianType.Contact);

                // Jacobian verification
                Assert.AreApproximatelyEqual(jacobian.CoefficientOfFriction, 0.5f, 0.01f);
                Assert.IsFalse(jacobian.Modified);
                Assert.AreEqual(jacobian.NumContacts, 4);
                for (int i = 0; i < jacobian.NumContacts; i++)
                {
                    ContactJacAngAndVelToReachCp jacAng = header.GetAngularJacobian(i);
                    Assert.AreEqual(jacAng.Jac.Impulse, 0.0f);
                }
            }

            public void Execute(ref ModifiableJacobianHeader header, ref ModifiableTriggerJacobian jacobian) { }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            SimulationCallbacks.Callback verifyJacobiansIteratorJobCallback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
            {
                return new VerifyJacobiansIteratorJob
                {
                    Bodies = world.Bodies,
                    VerificationData = GetComponentDataFromEntity<VerifyJacobiansIteratorData>(true)
                }.Schedule(simulation, ref world, inDeps);
            };

            m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateContactJacobians, verifyJacobiansIteratorJobCallback, inputDeps);

            return inputDeps;
        }
    }
}
