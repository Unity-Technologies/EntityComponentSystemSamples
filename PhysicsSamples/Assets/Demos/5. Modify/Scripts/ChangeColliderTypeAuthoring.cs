using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Authoring;
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
[UpdateAfter(typeof(EndColliderBakingSystem))]
public partial class ChangeColliderTypeBakingSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var manager = EntityManager;
        foreach (var colliderType in SystemAPI.Query<RefRW<ChangeColliderType>>().WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities))
        {
            var colliderA = manager.GetComponentData<PhysicsCollider>(colliderType.ValueRW.EntityA);
            var colliderB = manager.GetComponentData<PhysicsCollider>(colliderType.ValueRW.EntityB);

            colliderType.ValueRW.ColliderA = colliderA;
            colliderType.ValueRW.ColliderB = colliderB;
        }
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
            foreach (var(modifier, entity) in SystemAPI.Query<RefRW<ChangeColliderType>>().WithEntityAccess().WithAll<PhysicsCollider, RenderMeshArray>())
            {
                modifier.ValueRW.LocalTime -= deltaTime;

                if (modifier.ValueRW.LocalTime > 0.0f) return;

                modifier.ValueRW.LocalTime = modifier.ValueRW.TimeToSwap;
                var collider = EntityManager.GetComponentData<PhysicsCollider>(entity);

                if (collider.ColliderPtr->Type == modifier.ValueRW.ColliderA.ColliderPtr->Type)
                {
                    commandBuffer.SetComponent(entity, modifier.ValueRW.ColliderB);
                    commandBuffer.SetComponent(entity, EntityManager.GetComponentData<MaterialMeshInfo>(modifier.ValueRW.EntityB));
                }
                else
                {
                    commandBuffer.SetComponent(entity, modifier.ValueRW.ColliderA);
                    commandBuffer.SetComponent(entity, EntityManager.GetComponentData<MaterialMeshInfo>(modifier.ValueRW.EntityA));
                }
            }

            commandBuffer.Playback(EntityManager);
        }
    }
}
