using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Physics.Extensions
{
    // Applies any mouse spring as a change in velocity on the entity's motion component
    [UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
    public partial class MouseSpringSystem : SystemBase
    {
        MousePickSystem m_PickSystem;

        protected override void OnCreate()
        {
            m_PickSystem = World.GetOrCreateSystemManaged<MousePickSystem>();
            RequireForUpdate<MousePick>();
        }

        protected override void OnUpdate()
        {
            ComponentLookup<LocalTransform> LocalTransforms = GetComponentLookup<LocalTransform>(true);

            ComponentLookup<PhysicsVelocity> Velocities = GetComponentLookup<PhysicsVelocity>();
            ComponentLookup<PhysicsMass> Masses = GetComponentLookup<PhysicsMass>(true);
            ComponentLookup<PhysicsMassOverride> MassOverrides = GetComponentLookup<PhysicsMassOverride>(true);

            // If there's a pick job, wait for it to finish
            if (m_PickSystem.PickJobHandle != null)
            {
                JobHandle.CombineDependencies(Dependency, m_PickSystem.PickJobHandle.Value).Complete();
            }

            // If there's a picked entity, drag it
            MousePickSystem.SpringData springData = m_PickSystem.SpringDataRef.Value;
            if (springData.Dragging)
            {
                Entity entity = springData.Entity;
                if (!Masses.HasComponent(entity))
                {
                    return;
                }

                PhysicsMass massComponent = Masses[entity];
                PhysicsVelocity velocityComponent = Velocities[entity];

                // if body is kinematic
                // TODO: you should be able to rotate a body with infinite mass but finite inertia
                if (massComponent.HasInfiniteMass || MassOverrides.HasComponent(entity) && MassOverrides[entity].IsKinematic != 0)
                {
                    return;
                }


                var worldFromBody = new Math.MTransform(LocalTransforms[entity].Rotation, LocalTransforms[entity].Position);


                // Body to motion transform
                var bodyFromMotion = new Math.MTransform(Masses[entity].InertiaOrientation, Masses[entity].CenterOfMass);
                Math.MTransform worldFromMotion = Math.Mul(worldFromBody, bodyFromMotion);

                // TODO: shouldn't damp where inertia mass or inertia
                // Damp the current velocity
                const float gain = 0.95f;
                velocityComponent.Linear *= gain;
                velocityComponent.Angular *= gain;

                // Get the body and mouse points in world space
                float3 pointBodyWs = Math.Mul(worldFromBody, springData.PointOnBody);
                float3 pointSpringWs = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, springData.MouseDepth));

                // Calculate the required change in velocity
                float3 pointBodyLs = Math.Mul(Math.Inverse(bodyFromMotion), springData.PointOnBody);
                float3 deltaVelocity;
                {
                    float3 pointDiff = pointBodyWs - pointSpringWs;
                    float3 relativeVelocityInWorld = velocityComponent.Linear + math.mul(worldFromMotion.Rotation, math.cross(velocityComponent.Angular, pointBodyLs));

                    const float elasticity = 0.1f;
                    const float damping = 0.5f;
                    deltaVelocity = -pointDiff * (elasticity / SystemAPI.Time.DeltaTime) - damping * relativeVelocityInWorld;
                }

                // Build effective mass matrix in world space
                // TODO how are bodies with inf inertia and finite mass represented
                // TODO the aggressive damping is hiding something wrong in this code if dragging non-uniform shapes
                float3x3 effectiveMassMatrix;
                {
                    float3 arm = pointBodyWs - worldFromMotion.Translation;
                    var skew = new float3x3(
                        new float3(0.0f, arm.z, -arm.y),
                        new float3(-arm.z, 0.0f, arm.x),
                        new float3(arm.y, -arm.x, 0.0f)
                    );

                    // world space inertia = worldFromMotion * inertiaInMotionSpace * motionFromWorld
                    var invInertiaWs = new float3x3(
                        massComponent.InverseInertia.x * worldFromMotion.Rotation.c0,
                        massComponent.InverseInertia.y * worldFromMotion.Rotation.c1,
                        massComponent.InverseInertia.z * worldFromMotion.Rotation.c2
                    );
                    invInertiaWs = math.mul(invInertiaWs, math.transpose(worldFromMotion.Rotation));

                    float3x3 invEffMassMatrix = math.mul(math.mul(skew, invInertiaWs), skew);
                    invEffMassMatrix.c0 = new float3(massComponent.InverseMass, 0.0f, 0.0f) - invEffMassMatrix.c0;
                    invEffMassMatrix.c1 = new float3(0.0f, massComponent.InverseMass, 0.0f) - invEffMassMatrix.c1;
                    invEffMassMatrix.c2 = new float3(0.0f, 0.0f, massComponent.InverseMass) - invEffMassMatrix.c2;

                    effectiveMassMatrix = math.inverse(invEffMassMatrix);
                }

                // Calculate impulse to cause the desired change in velocity
                float3 impulse = math.mul(effectiveMassMatrix, deltaVelocity);

                // Clip the impulse
                const float maxAcceleration = 250.0f;
                float maxImpulse = math.rcp(massComponent.InverseMass) * SystemAPI.Time.DeltaTime * maxAcceleration;
                impulse *= math.min(1.0f, math.sqrt((maxImpulse * maxImpulse) / math.lengthsq(impulse)));

                // Apply the impulse
                {
                    velocityComponent.Linear += impulse * massComponent.InverseMass;

                    float3 impulseLs = math.mul(math.transpose(worldFromMotion.Rotation), impulse);
                    float3 angularImpulseLs = math.cross(pointBodyLs, impulseLs);
                    velocityComponent.Angular += angularImpulseLs * massComponent.InverseInertia;
                }

                // Write back velocity
                Velocities[entity] = velocityComponent;
            }
        }
    }
}
