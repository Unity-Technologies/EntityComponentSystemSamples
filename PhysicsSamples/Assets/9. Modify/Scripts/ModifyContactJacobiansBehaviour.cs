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
public class ModifyContactJacobiansBehaviour : MonoBehaviour
{
    public ModifyContactJacobians.ModificationType ModificationType;
}

class ModifyContactJacobiansBaker : Baker<ModifyContactJacobiansBehaviour>
{
    public override void Bake(ModifyContactJacobiansBehaviour authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ModifyContactJacobians { type = authoring.ModificationType });
    }
}

[UpdateInGroup(typeof(PhysicsCreateJacobiansGroup), OrderFirst = true)]
public partial struct SetContactFlagsSystem : ISystem
{
    private ComponentLookup<ModifyContactJacobians> m_JacobianData;

    // This job reads the modify component and sets some data on the contact, to get propagated to the jacobian
    // for processing in our jacobian modifier job. This is necessary because some flags require extra data to
    // be allocated along with the jacobian (e.g., SurfaceVelocity data typically does not exist). We also set
    // user data bits in the jacobianFlags to save us from looking up the ComponentLookup later.
    [BurstCompile]
    struct SetContactFlagsJob : IContactsJob
    {
        [ReadOnly]
        public ComponentLookup<ModifyContactJacobians> modificationData;

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

            if (ModifyContactJacobiansSystem.IsModificationType(ModifyContactJacobians.ModificationType.SurfaceVelocity, typeA, typeB))
            {
                manifold.JacobianFlags |= JacobianFlags.EnableSurfaceVelocity;
            }

            if (ModifyContactJacobiansSystem.IsModificationType(ModifyContactJacobians.ModificationType.InfiniteInertia, typeA, typeB) ||
                ModifyContactJacobiansSystem.IsModificationType(ModifyContactJacobians.ModificationType.BiggerInertia, typeA, typeB))
            {
                manifold.JacobianFlags |= JacobianFlags.EnableMassFactors;
            }
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadOnly<ModifyContactJacobians>()));
        m_JacobianData = state.GetComponentLookup<ModifyContactJacobians>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        m_JacobianData.Update(ref state);
        var simulationSingleton = SystemAPI.GetSingletonRW<SimulationSingleton>().ValueRW;

        if (simulationSingleton.Type == SimulationType.NoPhysics)
        {
            return;
        }

        // Schedule jobs
        var world = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

        var job = new SetContactFlagsJob
        {
            modificationData = m_JacobianData
        };

        state.Dependency = job.Schedule(simulationSingleton, ref world, state.Dependency);
    }
}

// A system which configures the simulation step to modify contact jacobains in various ways
[UpdateInGroup(typeof(PhysicsSolveAndIntegrateGroup), OrderFirst = true)]
public partial struct ModifyContactJacobiansSystem : ISystem
{
    private ComponentLookup<ModifyContactJacobians> m_JacobianData;

    internal static bool IsModificationType(ModifyContactJacobians.ModificationType typeToCheck,
        ModifyContactJacobians.ModificationType typeOfA, ModifyContactJacobians.ModificationType typeOfB) => typeOfA == typeToCheck || typeOfB == typeToCheck;

    [BurstCompile]
    struct ModifyJacobiansJob : IJacobiansJob
    {
        [ReadOnly]
        public ComponentLookup<ModifyContactJacobians> modificationData;

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

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadOnly<ModifyContactJacobians>()));
        m_JacobianData = state.GetComponentLookup<ModifyContactJacobians>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        m_JacobianData.Update(ref state);
        var simulationSingleton = SystemAPI.GetSingletonRW<SimulationSingleton>().ValueRW;

        if (simulationSingleton.Type == SimulationType.NoPhysics)
        {
            return;
        }

        // Schedule jobs
        var world = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

        var job = new ModifyJacobiansJob
        {
            modificationData = m_JacobianData
        };

        state.Dependency = job.Schedule(simulationSingleton, ref world, state.Dependency);
    }
}
