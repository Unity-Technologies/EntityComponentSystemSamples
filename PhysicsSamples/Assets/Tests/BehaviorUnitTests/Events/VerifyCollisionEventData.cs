using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public struct VerifyCollisionEventDataData : IComponentData
    {
    }

    [Serializable]
    public class VerifyCollisionEventData : MonoBehaviour
    {
        class VerifyCollisionEventDataBaker : Baker<VerifyCollisionEventData>
        {
            public override void Bake(VerifyCollisionEventData authoring)
            {
                AddComponent(new VerifyCollisionEventDataData());

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
    public partial class VerifyCollisionEventDataSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<VerifyCollisionEventDataData>();
        }

        struct VerifyCollisionEventDataJob : ICollisionEventsJob
        {
            [ReadOnly]
            public PhysicsWorld World;

            [ReadOnly]
            public ComponentLookup<VerifyCollisionEventDataData> VerificationData;

            public void Execute(CollisionEvent collisionEvent)
            {
                // Collision event is between a static and dynamic box.
                // Verify all data in the provided event struct.
                CollisionEvent.Details details = collisionEvent.CalculateDetails(ref World);
                Assert.IsTrue(details.EstimatedImpulse >= 0.0f);
                Assert.IsTrue(details.EstimatedContactPointPositions.Length == 4);
                Assert.AreNotEqual(collisionEvent.BodyIndexA, collisionEvent.BodyIndexB);
                Assert.AreEqual(collisionEvent.ColliderKeyA.Value, ColliderKey.Empty.Value);
                Assert.AreEqual(collisionEvent.ColliderKeyB.Value, ColliderKey.Empty.Value);
                Assert.AreEqual(collisionEvent.EntityA, World.Bodies[collisionEvent.BodyIndexA].Entity);
                Assert.AreEqual(collisionEvent.EntityB, World.Bodies[collisionEvent.BodyIndexB].Entity);
                Assert.AreApproximatelyEqual(collisionEvent.Normal.x, 0.0f, 0.01f);
                Assert.AreApproximatelyEqual(collisionEvent.Normal.y, 1.0f, 0.01f);
                Assert.AreApproximatelyEqual(collisionEvent.Normal.z, 0.0f, 0.01f);
            }
        }

        protected override void OnUpdate()
        {
            Dependency = new VerifyCollisionEventDataJob
            {
                World = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
                VerificationData = GetComponentLookup<VerifyCollisionEventDataData>(true)
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);
        }
    }
}
