using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public struct VerifyJacobiansIteratorData : IComponentData
    {
    }

    [Serializable]
    public class VerifyJacobiansIterator : MonoBehaviour
    {
        class VerifyJacobiansIteratorBaker : Baker<VerifyJacobiansIterator>
        {
            public override void Bake(VerifyJacobiansIterator authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<VerifyJacobiansIteratorData>(entity);

#if HAVOK_PHYSICS_EXISTS
                Havok.Physics.HavokConfiguration config = Havok.Physics.HavokConfiguration.Default;
                config.EnableSleeping = 0;
                AddComponent(entity, config);
#endif
            }
        }
    }
    [UpdateInGroup(typeof(PhysicsSimulationGroup))]
    [UpdateAfter(typeof(PhysicsCreateJacobiansGroup))]
    [UpdateBefore(typeof(PhysicsSolveAndIntegrateGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial struct VerifyJacobiansIteratorSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyJacobiansIteratorData) }
            }));
        }

        struct VerifyJacobiansIteratorJob : IJacobiansJob
        {
            [ReadOnly]
            public NativeArray<RigidBody> Bodies;

            [ReadOnly]
            public ComponentLookup<VerifyJacobiansIteratorData> VerificationData;

            public void Execute(ref ModifiableJacobianHeader header, ref ModifiableContactJacobian jacobian)
            {
                // Header verification
                Assert.IsFalse(header.AngularChanged);
                Assert.AreNotEqual(header.BodyIndexA, header.BodyIndexB);
                Assert.AreEqual(header.EntityA, Bodies[header.BodyIndexA].Entity);
                Assert.AreEqual(header.EntityB, Bodies[header.BodyIndexB].Entity);
                Assert.AreEqual(header.Flags, (JacobianFlags)0);
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

            public void Execute(ref ModifiableJacobianHeader header, ref ModifiableTriggerJacobian jacobian) {}
        }

        public void OnUpdate(ref SystemState state)
        {
            var worldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            state.Dependency = new VerifyJacobiansIteratorJob
            {
                Bodies = worldSingleton.PhysicsWorld.Bodies,
                VerificationData = SystemAPI.GetComponentLookup<VerifyJacobiansIteratorData>(true)
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), ref worldSingleton.PhysicsWorld, state.Dependency);
        }
    }
}
