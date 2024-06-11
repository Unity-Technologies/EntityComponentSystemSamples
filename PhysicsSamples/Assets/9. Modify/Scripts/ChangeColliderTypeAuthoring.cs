using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using UnityEngine;

public struct ChangeColliderType : IComponentData
{
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
            EntityA = entityA,
            EntityB = entityB,
            TimeToSwap = authoring.TimeToSwap,
            LocalTime = authoring.TimeToSwap,
        });
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
                if (modifier.ValueRW.LocalTime > 0.0f) continue;
                modifier.ValueRW.LocalTime = modifier.ValueRW.TimeToSwap;

                var collider = state.EntityManager.GetComponentData<PhysicsCollider>(entity);
                unsafe
                {
                    MaterialMeshInfo updateMeshInfo;
                    var  updateCollider = state.EntityManager.GetComponentData<PhysicsCollider>(modifier.ValueRW.EntityA);

                    if (collider.ColliderPtr->Type == updateCollider.ColliderPtr->Type)
                    {
                        updateCollider =
                            state.EntityManager.GetComponentData<PhysicsCollider>(modifier.ValueRW.EntityB);
                        updateMeshInfo =
                            state.EntityManager.GetComponentData<MaterialMeshInfo>(modifier.ValueRW.EntityB);
                    }
                    else
                    {
                        // keep updateCollider as-is
                        updateMeshInfo =
                            state.EntityManager.GetComponentData<MaterialMeshInfo>(modifier.ValueRW.EntityA);
                    }
                    commandBuffer.SetComponent(entity, updateCollider);
                    commandBuffer.SetComponent(entity, updateMeshInfo);
                }
            }

            commandBuffer.Playback(state.EntityManager);
        }
    }
}
