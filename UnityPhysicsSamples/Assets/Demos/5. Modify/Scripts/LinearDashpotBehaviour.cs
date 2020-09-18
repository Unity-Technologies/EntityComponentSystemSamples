using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;
using Unity.Jobs;
using Unity.Transforms;

/*
 * Issues:
 *  - setting up constraints if not using GameObjects
 *  - providing utility functions for Component and Direct data manipulation
 *  - assigning multiple Components of the same type to a single Entity
 */

public struct LinearDashpot : IComponentData
{
    public Entity localEntity;
    public float3 localOffset;
    public Entity parentEntity;
    public float3 parentOffset;

    public int dontApplyImpulseToParent;
    public float strength;
    public float damping;
}

public class LinearDashpotBehaviour : MonoBehaviour, IConvertGameObjectToEntity
{
    public PhysicsBodyAuthoring parentBody;
    public float3 parentOffset;
    public float3 localOffset;

    public bool dontApplyImpulseToParent = false;
    public float strength;
    public float damping;

    void OnEnable() {}

    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (enabled)
        {
            // Note: GetPrimaryEntity currently creates a new Entity
            //       if the parentBody is not a child in the scene hierarchy
            var componentData = new LinearDashpot
            {
                localEntity = entity,
                localOffset = localOffset,
                parentEntity = parentBody == null ? Entity.Null : conversionSystem.GetPrimaryEntity(parentBody),
                parentOffset = parentOffset,
                dontApplyImpulseToParent = dontApplyImpulseToParent ? 1 : 0,
                strength = strength,
                damping = damping
            };

            ComponentType[] componentTypes = new ComponentType[] { typeof(LinearDashpot) };
            Entity dashpotEntity = dstManager.CreateEntity(componentTypes);

#if UNITY_EDITOR
            var nameEntityA = dstManager.GetName(componentData.localEntity);
            var nameEntityB = dstManager.GetName(componentData.parentEntity);
            dstManager.SetName(dashpotEntity, $"LinearDashpot({nameEntityA},{nameEntityB})");
#endif
            dstManager.SetComponentData(dashpotEntity, componentData);
        }
    }
}

#region System
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(BuildPhysicsWorld))]
public class LinearDashpotSystem : SystemBase
{
    BuildPhysicsWorld m_BuildPhysicsWorldSystem;

    protected override void OnCreate()
    {
        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
    }

    protected override void OnUpdate()
    {
        Entities
            .WithName("LinearDashpotUpdate")
            .WithBurst()
            .ForEach((in LinearDashpot dashpot) =>
            {
                if (0 == dashpot.strength) return;

                var eA = dashpot.localEntity;
                var eB = dashpot.parentEntity;

                var eAIsNull = eA == Entity.Null;
                if (eAIsNull) return;
                var eBIsNull = eB == Entity.Null;

                var hasVelocityA = !eAIsNull && HasComponent<PhysicsVelocity>(eA);
                var hasVelocityB = !eBIsNull && HasComponent<PhysicsVelocity>(eB);

                if (!hasVelocityA) return;

                Translation positionA = default;
                Rotation rotationA = new Rotation { Value = quaternion.identity };
                PhysicsVelocity velocityA = default;
                PhysicsMass massA = default;

                Translation positionB = positionA;
                Rotation rotationB = rotationA;
                PhysicsVelocity velocityB = velocityA;
                PhysicsMass massB = massA;

                if (HasComponent<Translation>(eA)) positionA = GetComponent<Translation>(eA);
                if (HasComponent<Rotation>(eA)) rotationA = GetComponent<Rotation>(eA);
                if (HasComponent<PhysicsVelocity>(eA)) velocityA = GetComponent<PhysicsVelocity>(eA);
                if (HasComponent<PhysicsMass>(eA)) massA = GetComponent<PhysicsMass>(eA);

                if (HasComponent<LocalToWorld>(eB))
                {
                    // parent could be static and not have a Translation or Rotation
                    var worldFromBody = Math.DecomposeRigidBodyTransform(GetComponent<LocalToWorld>(eB).Value);
                    positionB = new Translation { Value = worldFromBody.pos };
                    rotationB = new Rotation { Value = worldFromBody.rot };
                }
                if (HasComponent<Translation>(eB)) positionB = GetComponent<Translation>(eB);
                if (HasComponent<Rotation>(eB)) rotationB = GetComponent<Rotation>(eB);
                if (HasComponent<PhysicsVelocity>(eB)) velocityB = GetComponent<PhysicsVelocity>(eB);
                if (HasComponent<PhysicsMass>(eB)) massB = GetComponent<PhysicsMass>(eB);


                var posA = math.transform(new RigidTransform(rotationA.Value, positionA.Value), dashpot.localOffset);
                var posB = math.transform(new RigidTransform(rotationB.Value, positionB.Value), dashpot.parentOffset);
                var lvA = velocityA.GetLinearVelocity(massA, positionA, rotationA, posA);
                var lvB = velocityB.GetLinearVelocity(massB, positionB, rotationB, posB);

                var impulse = dashpot.strength * (posB - posA) + dashpot.damping * (lvB - lvA);
                impulse = math.clamp(impulse, new float3(-100.0f), new float3(100.0f));

                velocityA.ApplyImpulse(massA, positionA, rotationA, impulse, posA);
                SetComponent(eA, velocityA);

                if (0 == dashpot.dontApplyImpulseToParent && hasVelocityB)
                {
                    velocityB.ApplyImpulse(massB, positionB, rotationB, -impulse, posB);
                    SetComponent(eB, velocityB);
                }
            }).Schedule();

        m_BuildPhysicsWorldSystem.AddInputDependency(Dependency);
    }
}
#endregion
