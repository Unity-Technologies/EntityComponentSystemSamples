using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

public struct ModifyContactJacobians : IComponentData
{
    public enum ModificationType
    {
        None,
        SoftContact,
        SurfaceVelocity,
        InfiniteInertia,
        BiggerInertia,
        NoAngularEffects,
        DisabledContact,
        DisabledAngularFriction,
    }

    public ModificationType type;
}

[Serializable]
public class ModifyContactJacobiansBehaviour : MonoBehaviour, IConvertGameObjectToEntity
{
    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new ModifyContactJacobians { type = ModificationType });
    }

    public ModifyContactJacobians.ModificationType ModificationType;
}

// A system which configures the simulation step to modify contact jacobains in various ways
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(StepPhysicsWorld))]
public class ModifyContactJacobiansSystem : SystemBase
{
    StepPhysicsWorld m_StepPhysicsWorld;
    SimulationCallbacks.Callback m_PreparationCallback;
    SimulationCallbacks.Callback m_JacobianModificationCallback;

    private static bool IsModificationType(ModifyContactJacobians.ModificationType typeToCheck,
        ModifyContactJacobians.ModificationType typeOfA, ModifyContactJacobians.ModificationType typeOfB) => typeOfA == typeToCheck || typeOfB == typeToCheck;

    protected override void OnCreate()
    {
        m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();

        m_PreparationCallback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
        {
            return new SetContactFlagsJob
            {
                modificationData = GetComponentDataFromEntity<ModifyContactJacobians>(true)
            }.Schedule(simulation, ref world, inDeps);
        };

        m_JacobianModificationCallback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
        {
            return new ModifyJacobiansJob
            {
                modificationData = GetComponentDataFromEntity<ModifyContactJacobians>(true)
            }.Schedule(simulation, ref world, inDeps);
        };

        RequireForUpdate(GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(ModifyContactJacobians) }
        }));
    }

    // This job reads the modify component and sets some data on the contact, to get propagated to the jacobian
    // for processing in our jacobian modifier job. This is necessary because some flags require extra data to
    // be allocated along with the jacobian (e.g., SurfaceVelocity data typically does not exist). We also set
    // user data bits in the jacobianFlags to save us from looking up the ComponentDataFromEntity later.
    [BurstCompile]
    struct SetContactFlagsJob : IContactsJob
    {
        [ReadOnly]
        public ComponentDataFromEntity<ModifyContactJacobians> modificationData;

        public void Execute(ref ModifiableContactHeader manifold, ref ModifiableContactPoint contact)
        {
            Entity entityA = manifold.EntityA;
            Entity entityB = manifold.EntityB;

            ModifyContactJacobians.ModificationType typeA = ModifyContactJacobians.ModificationType.None;
            if (modificationData.HasComponent(entityA))
            {
                typeA = modificationData[entityA].type;
            }

            ModifyContactJacobians.ModificationType typeB = ModifyContactJacobians.ModificationType.None;
            if (modificationData.HasComponent(entityB))
            {
                typeB = modificationData[entityB].type;
            }

            if (IsModificationType(ModifyContactJacobians.ModificationType.SurfaceVelocity, typeA, typeB))
            {
                manifold.JacobianFlags |= JacobianFlags.EnableSurfaceVelocity;
            }

            if (IsModificationType(ModifyContactJacobians.ModificationType.InfiniteInertia, typeA, typeB) ||
                IsModificationType(ModifyContactJacobians.ModificationType.BiggerInertia, typeA, typeB))
            {
                manifold.JacobianFlags |= JacobianFlags.EnableMassFactors;
            }
        }
    }

    [BurstCompile]
    struct ModifyJacobiansJob : IJacobiansJob
    {
        [ReadOnly]
        public ComponentDataFromEntity<ModifyContactJacobians> modificationData;

        // Don't do anything for triggers
        public void Execute(ref ModifiableJacobianHeader h, ref ModifiableTriggerJacobian j) {}

        public void Execute(ref ModifiableJacobianHeader jacHeader, ref ModifiableContactJacobian contactJacobian)
        {
            Entity entityA = jacHeader.EntityA;
            Entity entityB = jacHeader.EntityB;

            ModifyContactJacobians.ModificationType typeA = ModifyContactJacobians.ModificationType.None;
            if (modificationData.HasComponent(entityA))
            {
                typeA = modificationData[entityA].type;
            }

            ModifyContactJacobians.ModificationType typeB = ModifyContactJacobians.ModificationType.None;
            if (modificationData.HasComponent(entityB))
            {
                typeB = modificationData[entityB].type;
            }

            {
                // Check for jacobians we want to ignore:
                if (IsModificationType(ModifyContactJacobians.ModificationType.DisabledContact, typeA, typeB))
                {
                    jacHeader.Flags = jacHeader.Flags | JacobianFlags.Disabled;
                }

                // Check if NoTorque modifier, or friction should be disabled through jacobian
                if (IsModificationType(ModifyContactJacobians.ModificationType.NoAngularEffects, typeA, typeB) ||
                    IsModificationType(ModifyContactJacobians.ModificationType.DisabledAngularFriction, typeA, typeB))
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
                if (jacHeader.HasSurfaceVelocity && IsModificationType(ModifyContactJacobians.ModificationType.SurfaceVelocity, typeA, typeB))
                {
                    // Since surface normal can change, make sure angular velocity is always relative to it, not independent
                    jacHeader.SurfaceVelocity = new SurfaceVelocity
                    {
                        LinearVelocity = float3.zero,
                        AngularVelocity = contactJacobian.Normal * (new float3(0.0f, 1.0f, 0.0f))
                    };
                }

                // Check if MassFactors present and we should make inertia infinite
                if (jacHeader.HasMassFactors && IsModificationType(ModifyContactJacobians.ModificationType.InfiniteInertia, typeA, typeB))
                {
                    // Give both bodies infinite inertia
                    jacHeader.MassFactors = new MassFactors
                    {
                        InverseInertiaFactorA = float3.zero,
                        InverseMassFactorA = 1.0f,
                        InverseInertiaFactorB = float3.zero,
                        InverseMassFactorB = 1.0f
                    };
                }

                // Check if MassFactors present and we should make inertia 10x bigger
                if (jacHeader.HasMassFactors && IsModificationType(ModifyContactJacobians.ModificationType.BiggerInertia, typeA, typeB))
                {
                    // Give both bodies 10x bigger inertia
                    jacHeader.MassFactors = new MassFactors
                    {
                        InverseInertiaFactorA = new float3(0.1f),
                        InverseMassFactorA = 1.0f,
                        InverseInertiaFactorB = new float3(0.1f),
                        InverseMassFactorB = 1.0f
                    };
                }
            }

            // Angular jacobian modifications
            for (int i = 0; i < contactJacobian.NumContacts; i++)
            {
                ContactJacAngAndVelToReachCp jacobianAngular = jacHeader.GetAngularJacobian(i);

                // Check if NoTorque modifier
                if (IsModificationType(ModifyContactJacobians.ModificationType.NoAngularEffects, typeA, typeB))
                {
                    // Disable all angular effects
                    jacobianAngular.Jac.AngularA = 0.0f;
                    jacobianAngular.Jac.AngularB = 0.0f;
                }

                // Check if SoftContact modifier
                if (IsModificationType(ModifyContactJacobians.ModificationType.SoftContact, typeA, typeB))
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

    protected override void OnUpdate()
    {
        if (m_StepPhysicsWorld.Simulation.Type == SimulationType.NoPhysics) return;

        m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateContacts, m_PreparationCallback, Dependency);
        m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateContactJacobians, m_JacobianModificationCallback, Dependency);
    }
}
