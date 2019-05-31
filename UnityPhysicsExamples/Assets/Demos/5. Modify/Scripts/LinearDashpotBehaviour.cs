using System;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

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
    public PhysicsBody parentBody;
    public float3 parentOffset;
    public float3 localOffset;

    public bool dontApplyImpulseToParent = false;
    public float strength;
    public float damping;

    void OnEnable() { }

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
[UpdateAfter(typeof(BuildPhysicsWorld)), UpdateBefore(typeof(StepPhysicsWorld))]
public class LinearDashpotSystem : ComponentSystem
{
    BuildPhysicsWorld m_BuildPhysicsWorldSystem;

    protected override void OnCreate()
    {
        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
    }

    protected override void OnUpdate()
    {
        // Make sure the world has finished building before querying it
        m_BuildPhysicsWorldSystem.FinalJobHandle.Complete();

        Entities.ForEach( (ref LinearDashpot dashpot) =>
        {
            if (0 == dashpot.strength)
                return;

            var eA = dashpot.localEntity;
            var eB = dashpot.parentEntity;

            var world = m_BuildPhysicsWorldSystem.PhysicsWorld;

            // Find the rigid bodies in the physics world
            int rbAIdx = world.GetRigidBodyIndex(eA);
            int rbBIdx = world.GetRigidBodyIndex(eB);

            // Calculate and apply the impulses
            RigidBody rbA = rbAIdx >= 0 ? world.Bodies[rbAIdx] : RigidBody.Zero;
            RigidBody rbB = rbBIdx >= 0 ? world.Bodies[rbBIdx] : RigidBody.Zero;

            var posA = math.transform(rbA.WorldFromBody, dashpot.localOffset);
            var posB = math.transform(rbB.WorldFromBody, dashpot.parentOffset);
            var lvA = world.GetLinearVelocity(rbAIdx, posA);
            var lvB = world.GetLinearVelocity(rbBIdx, posB);

            var impulse = dashpot.strength * (posB - posA) + dashpot.damping * (lvB - lvA);
            impulse = math.clamp(impulse, new float3(-100.0f), new float3(100.0f));

            world.ApplyImpulse(rbAIdx, impulse, posA);
            if (0 == dashpot.dontApplyImpulseToParent)
                world.ApplyImpulse(rbBIdx, -impulse, posB);
        });
    }
}
#endregion

