using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Systems;
using Unity.Rendering;
using UnityEngine;

public struct ChangeMotionType : IComponentData
{
    public Entity EntityDynamic;
    public Entity EntityKinematic;
    public Entity EntityStatic;

    public PhysicsCollider DynamicCollider;
    public PhysicsMass DynamicMass;
    public PhysicsVelocity DynamicVelocity;
    public float TimeToSwap;
    internal float LocalTime;
    public BodyMotionType MotionType;
}

// Converted in PhysicsSamplesConversionSystem so Physics and Graphics conversion is complete
public class ChangeMotionTypeBehaviour : MonoBehaviour, IDeclareReferencedPrefabs//, IConvertGameObjectToEntity
{
    public GameObject DynamicBody;
    public GameObject KinematicBody;
    public GameObject StaticBody;

    [Range(0, 10)] public float TimeToSwap = 1.0f;


    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var collider = dstManager.GetComponentData<PhysicsCollider>(entity);
        var mass = dstManager.GetComponentData<PhysicsMass>(entity);
        var velocity = dstManager.GetComponentData<PhysicsVelocity>(entity);

        dstManager.AddComponentData(entity, new ChangeMotionType
        {
            EntityDynamic = conversionSystem.GetPrimaryEntity(DynamicBody),
            EntityKinematic = conversionSystem.GetPrimaryEntity(KinematicBody),
            EntityStatic = conversionSystem.GetPrimaryEntity(StaticBody),

            DynamicCollider = collider,
            DynamicMass = mass,
            DynamicVelocity = velocity,
            TimeToSwap = TimeToSwap,
            LocalTime = TimeToSwap,
            MotionType = BodyMotionType.Dynamic,
        });
    }

    // ensure these prefabs have been converted before we need them
    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(DynamicBody);
        referencedPrefabs.Add(KinematicBody);
        referencedPrefabs.Add(StaticBody);
    }
}



[UpdateBefore(typeof(BuildPhysicsWorld))]
public class ChangeMotionTypeSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.WithAll<ChangeMotionType, RenderMesh>().ForEach(
            (Entity entity, ref ChangeMotionType modifier) =>
        {
            modifier.LocalTime -= UnityEngine.Time.deltaTime;

            if (modifier.LocalTime > 0.0f) return;

            modifier.LocalTime = modifier.TimeToSwap;

            var renderMesh = World.EntityManager.GetSharedComponentData<RenderMesh>(entity);
            UnityEngine.Material material = renderMesh.material;
            switch (modifier.MotionType)
            {
                case BodyMotionType.Dynamic:
                    // move to kinematic body by removing PhysicsMass component
                    // note that a 'kinematic' body is really just a dynamic body with infinite mass properties
                    // hence the same thing could be achieved by setting properties via PhysicsMass.CreateKinematic
                    PostUpdateCommands.RemoveComponent<PhysicsMass>(entity);

                    // set the new modifier type and grab the appropriate new render material
                    material = World.EntityManager.GetSharedComponentData<RenderMesh>(modifier.EntityKinematic).material;
                    modifier.MotionType = BodyMotionType.Kinematic;
                    break;
                case BodyMotionType.Kinematic:
                    // move to static by removing PhysicsVelocity
                    PostUpdateCommands.RemoveComponent<PhysicsVelocity>(entity);

                    // set the new modifier type and grab the appropriate new render material
                    material = World.EntityManager.GetSharedComponentData<RenderMesh>(modifier.EntityStatic).material;
                    modifier.MotionType = BodyMotionType.Static;
                    break;
                case BodyMotionType.Static:
                    // move to dynamic by adding PhysicsVelocity and non infinite PhysicsMass
                    modifier.DynamicVelocity.Linear.y = 5.0f;
                    PostUpdateCommands.AddComponent(entity, modifier.DynamicVelocity);
                    PostUpdateCommands.AddComponent(entity, modifier.DynamicMass);

                    // set the new modifier type and grab the appropriate new render material
                    material = World.EntityManager.GetSharedComponentData<RenderMesh>(modifier.EntityDynamic).material;
                    modifier.MotionType = BodyMotionType.Dynamic;
                    break;
            }
            // assign the new render mesh material
            renderMesh.material = material;
            PostUpdateCommands.SetSharedComponent(entity, renderMesh);
    });
    }
}
