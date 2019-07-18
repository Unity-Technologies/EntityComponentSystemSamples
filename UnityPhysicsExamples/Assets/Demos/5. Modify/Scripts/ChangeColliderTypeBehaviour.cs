using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using SphereCollider = Unity.Physics.SphereCollider;

public struct ChangeColliderType : IComponentData
{
    public PhysicsCollider ColliderA;
    public PhysicsCollider ColliderB;
    public Entity EntityA;
    public Entity EntityB;
    public float TimeToSwap;
    internal float LocalTime;
}

// Converted in PhysicsSamplesConversionSystem so Physics and Graphics conversion is over
public class ChangeColliderTypeBehaviour : MonoBehaviour, IDeclareReferencedPrefabs//, IConvertGameObjectToEntity
{
    public GameObject PhysicsColliderPrefabA;
    public GameObject PhysicsColliderPrefabB;
    [Range(0, 10)] public float TimeToSwap = 1.0f;


    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (PhysicsColliderPrefabA == null || PhysicsColliderPrefabB == null) return;

        var entityA = conversionSystem.GetPrimaryEntity(PhysicsColliderPrefabA);
        var entityB = conversionSystem.GetPrimaryEntity(PhysicsColliderPrefabB);

        if (entityA == Entity.Null || entityB == Entity.Null) return;

        var colliderA = dstManager.GetComponentData<PhysicsCollider>(entityA);
        var colliderB = dstManager.GetComponentData<PhysicsCollider>(entityB);

        dstManager.AddComponentData(entity, new ChangeColliderType()
        {
            ColliderA = colliderA,
            ColliderB = colliderB,
            EntityA = entityA,
            EntityB = entityB,
            TimeToSwap = TimeToSwap,
            LocalTime = TimeToSwap,
        });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        if (PhysicsColliderPrefabA == null || PhysicsColliderPrefabB == null) return;

        referencedPrefabs.Add(PhysicsColliderPrefabA);
        referencedPrefabs.Add(PhysicsColliderPrefabB);
    }
}


[UpdateBefore(typeof(BuildPhysicsWorld))]
public class ChangeColliderTypeSystem : ComponentSystem
{
    protected unsafe override void OnUpdate()
    {
        var em = World.Active.EntityManager;

        Entities.WithAll<PhysicsCollider, ChangeColliderType, RenderMesh>().ForEach( 
            (Entity entity, ref ChangeColliderType modifier) =>
        {
            modifier.LocalTime -= Time.fixedDeltaTime;

            if (modifier.LocalTime > 0.0f) return;

            modifier.LocalTime = modifier.TimeToSwap;
            var collider = em.GetComponentData<PhysicsCollider>(entity);
            if (collider.ColliderPtr->Type == modifier.ColliderA.ColliderPtr->Type)
            {
                PostUpdateCommands.SetComponent(entity,modifier.ColliderB);
                PostUpdateCommands.SetSharedComponent(entity,em.GetSharedComponentData<RenderMesh>(modifier.EntityB));
            }
            else
            {
                PostUpdateCommands.SetComponent(entity, modifier.ColliderA);
                PostUpdateCommands.SetSharedComponent(entity, em.GetSharedComponentData<RenderMesh>(modifier.EntityA));
            }
        });
    }
}

