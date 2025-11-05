// This test verifies the CollisionEventData struct and also that the impulse accumulation for collision events
// involving a fast contact point (such as a sphere bouncing) is correct. When substepping>1, a bounce contact will happen
// during a single substep, and the impulse should be accumulated correctly across multiple substeps of the same frame.
// The test will fail if the impulse is zero. Test is hardcoded for a sphere with a single contact point
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public struct VerifyRestitutionCollisionEventDataData : IComponentData {}

    [Serializable]
    public class VerifyRestitutionCollisionEventData : MonoBehaviour
    {
        class VerifyRestitutionCollisionEventDataBaker : Baker<VerifyRestitutionCollisionEventData>
        {
            public override void Bake(VerifyRestitutionCollisionEventData authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new VerifyRestitutionCollisionEventDataData());

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
    public partial struct VerifyRestitutionCollisionEventDataSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VerifyRestitutionCollisionEventDataData>();
        }

        struct VerifyRestitutionCollisionEventDataJob : ICollisionEventsJob
        {
            [ReadOnly] public PhysicsWorld World;

            public void Execute(CollisionEvent collisionEvent)
            {
                // Verify all data in the provided event struct.
                CollisionEvent.Details details = collisionEvent.CalculateDetails(ref World);
                Assert.AreNotEqual(collisionEvent.BodyIndexA, collisionEvent.BodyIndexB);
                Assert.AreEqual(collisionEvent.ColliderKeyA.Value, ColliderKey.Empty.Value);
                Assert.AreEqual(collisionEvent.ColliderKeyB.Value, ColliderKey.Empty.Value);
                Assert.AreEqual(collisionEvent.EntityA, World.Bodies[collisionEvent.BodyIndexA].Entity);
                Assert.AreEqual(collisionEvent.EntityB, World.Bodies[collisionEvent.BodyIndexB].Entity);
                Assert.AreApproximatelyEqual(collisionEvent.Normal.x, 0.0f, 0.01f);
                Assert.AreApproximatelyEqual(collisionEvent.Normal.y, 1.0f, 0.01f);
                Assert.AreApproximatelyEqual(collisionEvent.Normal.z, 0.0f, 0.01f);

                // Sphere collider specific check:
                Assert.IsTrue(details.EstimatedContactPointPositions.Length == 1);

                // Impulse=zero would indicate there is a problem with the impulse accumulation in the contact jacobian
                Assert.IsTrue(details.EstimatedImpulse > 0.01f,  "Impulse should be non-zero");
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new VerifyRestitutionCollisionEventDataJob
            {
                World = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
        }
    }
}
