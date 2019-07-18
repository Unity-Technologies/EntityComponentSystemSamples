using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public struct VerifyContactsIteratorData : IComponentData
    {

    }

    [Serializable]
    public class VerifyContactsIterator : MonoBehaviour, IConvertGameObjectToEntity
    {
        void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new VerifyContactsIteratorData());
        }
    }

    [UpdateBefore(typeof(StepPhysicsWorld))]
    public class VerifyContactsIteratorSystem : JobComponentSystem
    {
        EntityQuery m_VerificationGroup;
        StepPhysicsWorld m_StepPhysicsWorld;

        public NativeArray<int> CurrentManifoldNumContacts;

        protected override void OnCreate()
        {
            m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            m_VerificationGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyContactsIteratorData) }
            });
            CurrentManifoldNumContacts = new NativeArray<int>(2, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            CurrentManifoldNumContacts.Dispose();
        }

        struct VerifyContactsIteratorJob : IContactsJob
        {
            private bool m_Initialized;

            public int NumContacts;
            public ModifiableContactHeader CurrentManifold;

            public NativeArray<int> CurrentManifoldNumContacts;

            [ReadOnly]
            public NativeSlice<RigidBody> Bodies;

            [ReadOnly]
            public ComponentDataFromEntity<VerifyContactsIteratorData> VerificationData;

            public void Execute(ref ModifiableContactHeader manifold, ref ModifiableContactPoint contact)
            {
                if (!m_Initialized)
                {
                    m_Initialized = true;
                    CurrentManifold = manifold;
                    NumContacts = 0;
                }

                // Header verification
                Assert.AreEqual(manifold.BodyCustomTags.CustomTagsA, (byte)0);
                Assert.AreEqual(manifold.BodyCustomTags.CustomTagsB, (byte)0);
                Assert.AreNotEqual(manifold.BodyIndexPair.BodyAIndex, manifold.BodyIndexPair.BodyBIndex);
                Assert.AreApproximatelyEqual(manifold.CoefficientOfFriction, 0.5f, 0.01f);
                Assert.AreApproximatelyEqual(manifold.CoefficientOfRestitution, 0.0f, 0.01f);
                Assert.AreEqual(manifold.ColliderKeys.ColliderKeyA.Value, ColliderKey.Empty.Value);
                Assert.AreEqual(manifold.ColliderKeys.ColliderKeyB.Value, ColliderKey.Empty.Value);
                Assert.AreEqual(manifold.Entities.EntityA, Bodies[manifold.BodyIndexPair.BodyAIndex].Entity);
                Assert.AreEqual(manifold.Entities.EntityB, Bodies[manifold.BodyIndexPair.BodyBIndex].Entity);
                Assert.AreEqual(manifold.Entities.EntityA.Version, 1);
                Assert.AreEqual(manifold.Entities.EntityB.Version, 1);
                Assert.AreEqual(manifold.JacobianFlags, (JacobianFlags)0);
                Assert.IsFalse(manifold.Modified);
                Assert.AreEqual(manifold.NumContacts, 4);

                NumContacts++;

                // Contact point verification
                Assert.IsTrue(contact.Index == NumContacts - 1);
                Assert.IsFalse(contact.Modified);

                // Save for later verification
                CurrentManifoldNumContacts[0] = CurrentManifold.NumContacts;
                CurrentManifoldNumContacts[1] = NumContacts;
            }
        }

        struct VerifyNumContactsJob : IJob
        {
            public NativeArray<int> CurrentManifoldNumContacts;

            [ReadOnly]
            public ComponentDataFromEntity<VerifyContactsIteratorData> VerificationData;

            public void Execute()
            {
                Assert.AreEqual(CurrentManifoldNumContacts[0], CurrentManifoldNumContacts[1]);
                Assert.AreEqual(CurrentManifoldNumContacts[0], 4);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            SimulationCallbacks.Callback verifyContactsIteratorJobCallback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
            {
                return new VerifyContactsIteratorJob
                {
                    CurrentManifoldNumContacts = CurrentManifoldNumContacts,
                    Bodies = world.Bodies,
                    VerificationData = GetComponentDataFromEntity<VerifyContactsIteratorData>(true)
                }.Schedule(simulation, ref world, inDeps);
            };

            SimulationCallbacks.Callback verifyNumContactsJobCallback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
            {
                return new VerifyNumContactsJob
                {
                    CurrentManifoldNumContacts = CurrentManifoldNumContacts,
                    VerificationData = GetComponentDataFromEntity<VerifyContactsIteratorData>(true)
                }.Schedule(inDeps);
            };

            m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateContacts, verifyContactsIteratorJobCallback, inputDeps);
            m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateContactJacobians, verifyNumContactsJobCallback, inputDeps);

            return inputDeps;
        }
    }
}
