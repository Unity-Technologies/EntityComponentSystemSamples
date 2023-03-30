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

        var entityA = GetEntity(authoring.PhysicsColliderPrefabA, TransformUsageFlags.Dynamic);
        var entityB = GetEntity(authoring.PhysicsColliderPrefabB, TransformUsageFlags.Dynamic);

        if (entityA == Entity.Null || entityB == Entity.Null) return;

        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ChangeColliderType()
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
public partial struct ChangeColliderTypeBakingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var manager = state.EntityManager;
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
public partial struct ChangeColliderTypeSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ChangeColliderType>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        using (var commandBuffer = new EntityCommandBuffer(Allocator.TempJob))
        {
            foreach (var(modifier, entity) in SystemAPI.Query<RefRW<ChangeColliderType>>().WithEntityAccess().WithAll<PhysicsCollider, RenderMeshArray>())
            {
                modifier.ValueRW.LocalTime -= deltaTime;

                if (modifier.ValueRW.LocalTime > 0.0f) return;

                modifier.ValueRW.LocalTime = modifier.ValueRW.TimeToSwap;
                var collider = state.EntityManager.GetComponentData<PhysicsCollider>(entity);
                unsafe
                {
                    if (collider.ColliderPtr->Type == modifier.ValueRW.ColliderA.ColliderPtr->Type)
                    {
                        commandBuffer.SetComponent(entity, modifier.ValueRW.ColliderB);
                        commandBuffer.SetComponent(entity, state.EntityManager.GetComponentData<MaterialMeshInfo>(modifier.ValueRW.EntityB));
                    }
                    else
                    {
                        commandBuffer.SetComponent(entity, modifier.ValueRW.ColliderA);
                        commandBuffer.SetComponent(entity, state.EntityManager.GetComponentData<MaterialMeshInfo>(modifier.ValueRW.EntityA));
                    }
                }
            }

            commandBuffer.Playback(state.EntityManager);
        }
    }
}
