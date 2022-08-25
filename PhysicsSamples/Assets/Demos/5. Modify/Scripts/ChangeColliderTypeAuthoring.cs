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
public class ChangeColliderTypeAuthoring : MonoBehaviour, IDeclareReferencedPrefabs//, IConvertGameObjectToEntity
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

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(BuildPhysicsWorld))]
public partial class ChangeColliderTypeSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate(GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(ChangeColliderType)
            }
        }));
    }

    protected override unsafe void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;
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
                        commandBuffer.SetSharedComponent(entity, EntityManager.GetSharedComponentData<RenderMesh>(modifier.EntityB));
                    }
                    else
                    {
                        commandBuffer.SetComponent(entity, modifier.ColliderA);
                        commandBuffer.SetSharedComponent(entity, EntityManager.GetSharedComponentData<RenderMesh>(modifier.EntityA));
                    }
                }).Run();

            commandBuffer.Playback(EntityManager);
        }
    }
}
