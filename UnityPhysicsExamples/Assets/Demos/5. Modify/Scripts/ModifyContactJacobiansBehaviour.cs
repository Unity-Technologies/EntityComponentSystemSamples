using System;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Math = Unity.Physics.Math;
using UnityEngine;

public struct ModifyContactJacobians : IComponentData { }

public class ModifyContactJacobiansBehaviour : MonoBehaviour, IConvertGameObjectToEntity
{
    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new ModifyContactJacobians());
    }
}

// A system which configures the simulation step to modify contact jacobains in various ways
[UpdateBefore(typeof(StepPhysicsWorld))]
public class ModifyContactJacobiansSystem : JobComponentSystem
{
    EntityQuery m_ContactModifierGroup;
    StepPhysicsWorld m_StepPhysicsWorld;

    protected override void OnCreate()
    {
        m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
        m_ContactModifierGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(ModifyContactJacobians) }
        });
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if(m_ContactModifierGroup.CalculateLength() == 0)
        {
            return inputDeps;
        }

        if (m_StepPhysicsWorld.Simulation.Type == SimulationType.NoPhysics)
        {
            return inputDeps;
        }

        SimulationCallbacks.Callback preparationCallback = (ref ISimulation simulation, JobHandle inDeps) =>
        {
            inDeps.Complete();  // shouldn't be needed (jobify the below)

            SimulationData.Contacts.Iterator iterator = simulation.Contacts.GetIterator();
            while (iterator.HasItemsLeft())
            {
                ContactHeader manifold = iterator.GetNextContactHeader();

                // JacobianModifierFlags format for this example
                // UserData 0 - soft contact
                // UserData 1 - surface velocity
                // UserData 2 - infinite inertia
                // UserData 3 - no torque
                // UserData 4 - clip impulse
                // UserData 5 - disabled contact

                if (0 != (manifold.BodyCustomDatas.CustomDataA & (byte)(1 << 0)) ||
                    0 != (manifold.BodyCustomDatas.CustomDataB & (byte)(1 << 0)))
                {
                    manifold.JacobianFlags |= JacobianFlags.UserFlag0; // Soft Contact
                }
                if (0 != (manifold.BodyCustomDatas.CustomDataA & (byte)(1 << 1)) ||
                    0 != (manifold.BodyCustomDatas.CustomDataB & (byte)(1 << 1)))
                {
                    manifold.JacobianFlags |= JacobianFlags.EnableSurfaceVelocity;
                }
                if (0 != (manifold.BodyCustomDatas.CustomDataA & (byte)(1 << 2)) ||
                    0 != (manifold.BodyCustomDatas.CustomDataB & (byte)(1 << 2)))
                {
                    manifold.JacobianFlags |= JacobianFlags.EnableMassFactors;
                }
                if (0 != (manifold.BodyCustomDatas.CustomDataA & (byte)(1 << 3)) ||
                    0 != (manifold.BodyCustomDatas.CustomDataB & (byte)(1 << 3)))
                {
                    manifold.JacobianFlags |= JacobianFlags.UserFlag1; // No Torque
                }
                if (0 != (manifold.BodyCustomDatas.CustomDataA & (byte)(1 << 4)) ||
                    0 != (manifold.BodyCustomDatas.CustomDataB & (byte)(1 << 4)))
                {
                    manifold.JacobianFlags |= JacobianFlags.EnableMaxImpulse; // No Torque
                }
                if (0 != (manifold.BodyCustomDatas.CustomDataA & (byte)(1 << 5)) ||
                    0 != (manifold.BodyCustomDatas.CustomDataB & (byte)(1 << 5)))
                {
                    manifold.JacobianFlags |= JacobianFlags.Disabled;
                }

                iterator.UpdatePreviousContactHeader(manifold);

                // Just read contacts
                for (int i = 0; i < manifold.NumContacts; i++)
                {
                    iterator.GetNextContact();
                }
            }

            return inDeps;
        };

        SimulationCallbacks.Callback jacobianModificationCallback = (ref ISimulation simulation, JobHandle inDeps) =>
        {
            inDeps.Complete();  // shouldn't be needed (jobify the below)

            JacobianIterator iterator = simulation.Jacobians.Iterator;
            while (iterator.HasJacobiansLeft())
            {
                // JacobianModifierFlags format for this example
                // UserFlag0 - soft contact
                // UserFlag1 - no torque

                // Jacobian header
                ref JacobianHeader jacHeader = ref iterator.ReadJacobianHeader();

                // Triggers can only be disabled, other modifiers have no effect
                if (jacHeader.Type == JacobianType.Contact)
                {
                    // Contact jacobian modification
                    ref ContactJacobian contactJacobian = ref jacHeader.AccessBaseJacobian<ContactJacobian>();
                    {
                        // Check if NoTorque modifier
                        if ((jacHeader.Flags & JacobianFlags.UserFlag1) != 0)
                        {
                            // Disable all friction angular effects
                            contactJacobian.Friction0.AngularA = 0.0f;
                            contactJacobian.Friction1.AngularA = 0.0f;
                            contactJacobian.AngularFriction.AngularA = 0.0f;
                            contactJacobian.Friction0.AngularB = 0.0f;
                            contactJacobian.Friction1.AngularB = 0.0f;
                            contactJacobian.AngularFriction.AngularB = 0.0f;
                        }

                        // Check if SurfaceVelocity present
                        if (jacHeader.HasSurfaceVelocity)
                        {
                            // Since surface normal can change, make sure angular velocity is always relative to it, not independent
                            float3 angVel = contactJacobian.BaseJacobian.Normal * (new float3(0.0f, 1.0f, 0.0f));
                            float3 linVel = float3.zero;

                            Math.CalculatePerpendicularNormalized(contactJacobian.BaseJacobian.Normal, out float3 dir0, out float3 dir1);
                            float linVel0 = math.dot(linVel, dir0);
                            float linVel1 = math.dot(linVel, dir1);

                            float angVelProj = math.dot(angVel, contactJacobian.BaseJacobian.Normal);

                            jacHeader.SurfaceVelocity = new SurfaceVelocity { ExtraFrictionDv = new float3(linVel0, linVel1, angVelProj) };
                        }

                        // Check if MaxImpulse present
                        if (jacHeader.HasMaxImpulse)
                        {
                            // Max impulse in Ns (Newton-second)
                            jacHeader.MaxImpulse = 20.0f;
                        }

                        // Check if MassFactors present
                        if (jacHeader.HasMassFactors)
                        {
                            // Give both bodies infinite inertia
                            jacHeader.MassFactors = new MassFactors
                            {
                                InvInertiaAndMassFactorA = new float4(0.0f, 0.0f, 0.0f, 1.0f),
                                InvInertiaAndMassFactorB = new float4(0.0f, 0.0f, 0.0f, 1.0f)
                            };
                        }
                    }

                    // Angular jacobian modifications
                    for (int i = 0; i < contactJacobian.BaseJacobian.NumContacts; i++)
                    {
                        ref ContactJacAngAndVelToReachCp jacobianAngular = ref jacHeader.AccessAngularJacobian(i);

                        // Check if NoTorque modifier
                        if ((jacHeader.Flags & JacobianFlags.UserFlag1) != 0)
                        {
                            // Disable all angular effects
                            jacobianAngular.Jac.AngularA = 0.0f;
                            jacobianAngular.Jac.AngularB = 0.0f;
                        }

                        // Check if SoftContact modifier
                        if ((jacHeader.Flags & JacobianFlags.UserFlag0) != 0)
                        {
                            jacobianAngular.Jac.EffectiveMass *= 0.1f;
                            if (jacobianAngular.VelToReachCp > 0.0f)
                            {
                                jacobianAngular.VelToReachCp *= 0.5f;
                            }
                        }
                    }
                }
            }

            return inDeps;
        };

        m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateContacts, preparationCallback);
        m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateContactJacobians, jacobianModificationCallback);

        return inputDeps;
    }
}
