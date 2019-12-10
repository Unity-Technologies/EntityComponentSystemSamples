using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public struct VerifyCollisionEventDataData : IComponentData
    {

    }

    [Serializable]
    public class VerifyCollisionEventData : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new VerifyCollisionEventDataData());
        }
    }

    [UpdateBefore(typeof(StepPhysicsWorld))]
    public class VerifyCollisionEventDataSystem : JobComponentSystem
    {
        EntityQuery m_VerificationGroup;
        StepPhysicsWorld m_StepPhysicsWorld;

        protected override void OnCreate()
        {
            m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            m_VerificationGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyCollisionEventDataData) }
            });
        }

        struct VerifyCollisionEventDataJob : ICollisionEventsJob
        {
            [ReadOnly]
            public PhysicsWorld World;

            [ReadOnly]
            public NativeSlice<RigidBody> Bodies;

            [ReadOnly]
            public ComponentDataFromEntity<VerifyCollisionEventDataData> VerificationData;

            public void Execute(CollisionEvent collisionEvent)
            {
                // Collision event is between a static and dynamic box.
                // Verify all data in the provided event struct.
                CollisionEvent.Details details = collisionEvent.CalculateDetails(ref World);
                Assert.IsTrue(details.EstimatedImpulse >= 0.0f);
                Assert.IsTrue(details.EstimatedContactPointPositions.Length == 4);
                Assert.AreNotEqual(collisionEvent.BodyIndices.BodyAIndex, collisionEvent.BodyIndices.BodyBIndex);
                Assert.AreEqual(collisionEvent.ColliderKeys.ColliderKeyA.Value, ColliderKey.Empty.Value);
                Assert.AreEqual(collisionEvent.ColliderKeys.ColliderKeyB.Value, ColliderKey.Empty.Value);
                Assert.AreEqual(collisionEvent.Entities.EntityA, Bodies[collisionEvent.BodyIndices.BodyAIndex].Entity);
                Assert.AreEqual(collisionEvent.Entities.EntityB, Bodies[collisionEvent.BodyIndices.BodyBIndex].Entity);
                Assert.AreEqual(collisionEvent.Entities.EntityA.Version, 1);
                Assert.AreEqual(collisionEvent.Entities.EntityB.Version, 1);
                Assert.AreApproximatelyEqual(collisionEvent.Normal.x, 0.0f, 0.01f);
                Assert.AreApproximatelyEqual(collisionEvent.Normal.y, 1.0f, 0.01f);
                Assert.AreApproximatelyEqual(collisionEvent.Normal.z, 0.0f, 0.01f);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            SimulationCallbacks.Callback testCollisionEventCallback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
            {
                return new VerifyCollisionEventDataJob
                {
                    World = world,
                    Bodies = world.Bodies,
                    VerificationData = GetComponentDataFromEntity<VerifyCollisionEventDataData>(true)
                }.Schedule(simulation, ref world, inDeps);
            };

            m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostSolveJacobians, testCollisionEventCallback, inputDeps);

            return inputDeps;
        }
    }
}
