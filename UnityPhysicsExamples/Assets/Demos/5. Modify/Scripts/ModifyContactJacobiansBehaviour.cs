using System;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Math = Unity.Physics.Math;
using UnityEngine;

public struct ModifyContactJacobians : IComponentData
{
    public enum ModificationType
    {
        None,
        SoftContact,
        SurfaceVelocity,
        InfiniteInertia,
        NoAngularEffects,
        ClippedImpulse,
        DisabledContact
    }

    public ModificationType type;
}

[Serializable]
public class ModifyContactJacobiansBehaviour : MonoBehaviour, IConvertGameObjectToEntity
{
    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new ModifyContactJacobians { type = ModificationType } );
    }


    public ModifyContactJacobians.ModificationType ModificationType;
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

    // This job reads the modify component and sets some data on the contact, to get propogated to the jacobian
    // for processing in our jacobian modifier job. This is necessary because some flags require extra data to
    // be allocated along with the jacobian (e.g., SurfaceVelocity data typically does not exist). We also set
    // user data bits in the jacobianFlags to save us from looking up the ComponentDataFromEntity later.
    struct SetContactFlagsJob : IContactsJob
    {
        [ReadOnly]
        public ComponentDataFromEntity<ModifyContactJacobians> modificationData;

        public void Execute(ref ModifiableContactHeader manifold, ref ModifiableContactPoint contact)
        {
            Entity entityA = manifold.Entities.EntityA;
            Entity entityB = manifold.Entities.EntityB;

            ModifyContactJacobians.ModificationType typeA = ModifyContactJacobians.ModificationType.None;
            if(modificationData.Exists(entityA))
            {
                typeA = modificationData[entityA].type;
            }

            ModifyContactJacobians.ModificationType typeB = ModifyContactJacobians.ModificationType.None;
            if(modificationData.Exists(entityB))
            {
                typeB = modificationData[entityB].type;
            }

            if (typeA == ModifyContactJacobians.ModificationType.SurfaceVelocity || typeB == ModifyContactJacobians.ModificationType.SurfaceVelocity)
            {
                manifold.JacobianFlags |= JacobianFlags.EnableSurfaceVelocity;
            }
            if (typeA == ModifyContactJacobians.ModificationType.InfiniteInertia || typeB == ModifyContactJacobians.ModificationType.InfiniteInertia)
            {
                manifold.JacobianFlags |= JacobianFlags.EnableMassFactors;
            }
            if (typeA == ModifyContactJacobians.ModificationType.ClippedImpulse || typeB == ModifyContactJacobians.ModificationType.ClippedImpulse)
            {
                manifold.JacobianFlags |= JacobianFlags.EnableMaxImpulse;
            }
        }
    }

    struct ModifyJacobiansJob : IJacobiansJob
    {
        [ReadOnly]
        public ComponentDataFromEntity<ModifyContactJacobians> modificationData;

        // Don't do anything for triggers
        public void Execute(ref ModifiableJacobianHeader h, ref ModifiableTriggerJacobian j){ }

        public void Execute(ref ModifiableJacobianHeader jacHeader, ref ModifiableContactJacobian contactJacobian)
        {
            Entity entityA = jacHeader.Entities.EntityA;
            Entity entityB = jacHeader.Entities.EntityB;

            ModifyContactJacobians.ModificationType typeA = ModifyContactJacobians.ModificationType.None;
            if (modificationData.Exists(entityA))
            {
                typeA = modificationData[entityA].type;
            }

            ModifyContactJacobians.ModificationType typeB = ModifyContactJacobians.ModificationType.None;
            if (modificationData.Exists(entityB))
            {
                typeB = modificationData[entityB].type;
            }

            {
                // Check for jacobians we want to ignore:
                if (typeA == ModifyContactJacobians.ModificationType.DisabledContact || typeB == ModifyContactJacobians.ModificationType.DisabledContact)
                {
                    jacHeader.Flags = jacHeader.Flags | JacobianFlags.Disabled;
                }

                // Check if NoTorque modifier
                if (typeA == ModifyContactJacobians.ModificationType.NoAngularEffects || typeB == ModifyContactJacobians.ModificationType.NoAngularEffects)
                {
                    // Disable all friction angular effects
                    var friction0 = contactJacobian.Friction0;
                    friction0.AngularA = 0.0f;
                    friction0.AngularB = 0.0f;
                    contactJacobian.Friction0 = friction0;

                    var friction1 = contactJacobian.Friction1;
                    friction1.AngularA = 0.0f;
                    friction1.AngularB = 0.0f;
                    contactJacobian.Friction1 = friction1;

                    var angularFriction = contactJacobian.AngularFriction;
                    angularFriction.AngularA = 0.0f;
                    angularFriction.AngularB = 0.0f;
                    contactJacobian.AngularFriction = angularFriction;
                }

                // Check if SurfaceVelocity present
                if (jacHeader.HasSurfaceVelocity &&
                    (typeA == ModifyContactJacobians.ModificationType.SurfaceVelocity || typeB == ModifyContactJacobians.ModificationType.SurfaceVelocity))
                {
                    // Since surface normal can change, make sure angular velocity is always relative to it, not independent
                    float3 angVel = contactJacobian.Normal * (new float3(0.0f, 1.0f, 0.0f));
                    float3 linVel = float3.zero;

                    Math.CalculatePerpendicularNormalized(contactJacobian.Normal, out float3 dir0, out float3 dir1);
                    float linVel0 = math.dot(linVel, dir0);
                    float linVel1 = math.dot(linVel, dir1);

                    float angVelProj = math.dot(angVel, contactJacobian.Normal);

                    jacHeader.SurfaceVelocity = new SurfaceVelocity { ExtraFrictionDv = new float3(linVel0, linVel1, angVelProj) };
                }

                // Check if MaxImpulse present
                if (jacHeader.HasMaxImpulse &&
                    (typeA == ModifyContactJacobians.ModificationType.ClippedImpulse || typeB == ModifyContactJacobians.ModificationType.ClippedImpulse))
                {
                    // Max impulse in Ns (Newton-second)
                    jacHeader.MaxImpulse = 20.0f;
                }

                // Check if MassFactors present
                if (jacHeader.HasMassFactors &&
                    (typeA == ModifyContactJacobians.ModificationType.InfiniteInertia || typeB == ModifyContactJacobians.ModificationType.InfiniteInertia))
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
            for (int i = 0; i < contactJacobian.NumContacts; i++)
            {
                ContactJacAngAndVelToReachCp jacobianAngular = jacHeader.GetAngularJacobian(i);

                // Check if NoTorque modifier
                if (typeA == ModifyContactJacobians.ModificationType.NoAngularEffects || typeB == ModifyContactJacobians.ModificationType.NoAngularEffects)
                {
                    // Disable all angular effects
                    jacobianAngular.Jac.AngularA = 0.0f;
                    jacobianAngular.Jac.AngularB = 0.0f;
                }

                // Check if SoftContact modifier
                if (typeA == ModifyContactJacobians.ModificationType.SoftContact || typeB == ModifyContactJacobians.ModificationType.SoftContact)
                {
                    jacobianAngular.Jac.EffectiveMass *= 0.1f;
                    if (jacobianAngular.VelToReachCp > 0.0f)
                    {
                        jacobianAngular.VelToReachCp *= 0.5f;
                    }
                }

                jacHeader.SetAngularJacobian(i, jacobianAngular);
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (m_StepPhysicsWorld.Simulation.Type == SimulationType.NoPhysics)
        {
            return inputDeps;
        }

        SimulationCallbacks.Callback preparationCallback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
        {
            return new SetContactFlagsJob
            {
                modificationData = GetComponentDataFromEntity<ModifyContactJacobians>(true)
            }.Schedule(simulation, ref world, inDeps);
        };

        SimulationCallbacks.Callback jacobianModificationCallback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
        {
            return new ModifyJacobiansJob
            {
                modificationData = GetComponentDataFromEntity<ModifyContactJacobians>(true)
            }.Schedule(simulation, ref world, inDeps);
        };

        m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateContacts, preparationCallback, inputDeps);
        m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateContactJacobians, jacobianModificationCallback, inputDeps);

        return inputDeps;
    }

}
