using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using UnityEngine;

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
public class ChangeColliderTypeAuthoring : MonoBehaviour
{
    public GameObject PhysicsColliderPrefabA;
    public GameObject PhysicsColliderPrefabB;
    [Range(0, 10)] public float TimeToSwap = 1.0f;
}

class ChangeColliderTypeAuthoringBaker : Baker<ChangeColliderTypeAuthoring>
{
    public override void Bake(ChangeColliderTypeAuthoring authoring)
    {
        if (authoring.PhysicsColliderPrefabA == null || authoring.PhysicsColliderPrefabB == null) return;

        var entityA = GetEntity(authoring.PhysicsColliderPrefabA);
        var entityB = GetEntity(authoring.PhysicsColliderPrefabB);

        if (entityA == Entity.Null || entityB == Entity.Null) return;

        AddComponent(new ChangeColliderType()
        {
            // These 2 are filled in in the baking system
            //ColliderA = colliderA,
            //ColliderB = colliderB,
            EntityA = entityA,
            EntityB = entityB,
            TimeToSwap = authoring.TimeToSwap,
            LocalTime = authoring.TimeToSwap,
        });
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
public partial class ChangeColliderTypeBakingSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var manager = EntityManager;
        Entities
            .ForEach((ref ChangeColliderType colliderType) =>
        {
            var colliderA = manager.GetComponentData<PhysicsCollider>(colliderType.EntityA);
            var colliderB = manager.GetComponentData<PhysicsCollider>(colliderType.EntityB);
            colliderType.ColliderA = colliderA;
            colliderType.ColliderB = colliderB;
        }).Run();
    }
}


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial class ChangeColliderTypeSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<ChangeColliderType>();
    }

    protected override unsafe void OnUpdate()
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        using (var commandBuffer = new EntityCommandBuffer(Allocator.TempJob))
        {
            Entities
                .WithName("ChangeColliderType")
                .WithAll<PhysicsCollider, RenderMesh>()
                .WithoutBurst()
                .ForEach((Entity entity, ref ChangeColliderType modifier) =>
                {
                    modifier.LocalTime -= deltaTime;

                    if (modifier.LocalTime > 0.0f) return;

                    modifier.LocalTime = modifier.TimeToSwap;
                    var collider = EntityManager.GetComponentData<PhysicsCollider>(entity);
                    if (collider.ColliderPtr->Type == modifier.ColliderA.ColliderPtr->Type)
                    {
                        commandBuffer.SetComponent(entity, modifier.ColliderB);
                        commandBuffer.SetSharedComponentManaged(entity, EntityManager.GetSharedComponentManaged<RenderMesh>(modifier.EntityB));
                    }
                    else
                    {
                        commandBuffer.SetComponent(entity, modifier.ColliderA);
                        commandBuffer.SetSharedComponentManaged(entity, EntityManager.GetSharedComponentManaged<RenderMesh>(modifier.EntityA));
                    }
                }).Run();

            commandBuffer.Playback(EntityManager);
        }
    }
}
