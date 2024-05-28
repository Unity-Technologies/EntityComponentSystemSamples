// This script is used in the `5g1. Change Collider Material - Bouncy Boxes` demo and it is based
// off the ChangeBoxColliderSizeAuthoring.cs script, but it expands on this behaviour by also
// changing the physics material properties based on if the box is growing or shrinking.
// The material (colour) is also changed to reflect modifications to the blob data.
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using BoxCollider = Unity.Physics.BoxCollider;

public struct ChangeColliderBlob : IComponentData
{
    public float3 Min;
    public float3 Max;
    public float3 Target;
}

public class ChangeColliderBlobAuthoring : MonoBehaviour
{
    public float3 Min = 0;
    public float3 Max = 10;
}

class ChangeColliderBaker : Baker<ChangeColliderBlobAuthoring>
{
    public override void Bake(ChangeColliderBlobAuthoring blobAuthoring)
    {
        var entity = GetEntity(TransformUsageFlags.ManualOverride);
        AddComponent(entity, new ChangeColliderBlob
        {
            Min = blobAuthoring.Min,
            Max = blobAuthoring.Max,
            Target = math.lerp(blobAuthoring.Min, blobAuthoring.Max, 0.5f),
        });

        AddComponent(entity, new PostTransformMatrix
        {
            Value = float4x4.identity,
        });
    }
}

/// <summary>
/// This system needs to run in the BeforePhysicsSystemGroup, after the EnsureUniqueColliderSystem. The
/// EnsureUniqueColliderSystem is responsible for updating unique collider flags on any prefabs. By running the
/// EnsureUniqueColliderSystem first, it ensures that the unique colliders are available when modifying the blobs here
/// </summary>
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
public partial struct ChangeColliderBlobSystem : ISystem
{
    const float k_GrowingRestitution = 0.75f;
    /// <summary>
    /// This job changes the size of the box collider (similar to ChangeBoxColliderSizeJob) but expands
    /// on it by also changing the physics material restitution.
    /// If the box is shrinking, then the restitution = 0
    /// If the box is growing, then the restitution = 0.75
    /// </summary>
    [BurstCompile]
    public partial struct ChangeColliderBlobJob : IJobEntity
    {
        public void Execute(ref PhysicsCollider collider, ref ChangeColliderBlob size,
            ref PostTransformMatrix postTransformMatrix)
        {
            // make sure we are dealing with boxes
            if (collider.Value.Value.Type != ColliderType.Box) return;

            float3 oldSize = 1.0f;
            float3 newSize = 1.0f;
            const float k_ShrinkingRestitution = 0.0f;

            unsafe
            {
                // Update the size of the box
                // grab the box pointer
                BoxCollider* bxPtr = (BoxCollider*)collider.ColliderPtr;
                oldSize = bxPtr->Size;
                newSize = math.lerp(oldSize, size.Target, 0.05f);

                // if we have reached the target size, get a new target
                float3 newTargetSize = math.select(size.Min, size.Max, size.Target == size.Min);
                size.Target = math.select(size.Target, newTargetSize,
                    math.abs(newSize - size.Target) < new float3(0.1f));

                var boxGeometry = bxPtr->Geometry;
                boxGeometry.Size = newSize;
                bxPtr->Geometry = boxGeometry;

                // Modify physics material restitution
                var oldRestitution = collider.Value.Value.GetRestitution();
                var newRestitution = oldRestitution;

                var sizeChange = CheckIfGrowing(oldSize, newSize);
                if (sizeChange > 0) //growing
                {
                    newRestitution = k_GrowingRestitution;
                }
                else if (sizeChange < 0) //shrinking
                {
                    newRestitution = k_ShrinkingRestitution;
                }
                //else leave it alone

                if (!newRestitution.Equals(oldRestitution))
                {
                    collider.Value.Value.SetRestitution(newRestitution);
                }
            }

            // now tweak the graphical representation of the box
            float3 newScale = newSize / oldSize;
            postTransformMatrix.Value.c0 *= newScale.x;
            postTransformMatrix.Value.c1 *= newScale.y;
            postTransformMatrix.Value.c2 *= newScale.z;

            if (!collider.IsUnique)
            {
                Debug.LogWarning($"Error: The collider {collider.Value.Value.Type} is not unique. Check your system order.");
            }
        }

        private int CheckIfGrowing(float3 oldSize, float3 newSize)
        {
            const float threshold = 0.0001f;
            var compare = newSize - oldSize;

            var sum = 0;
            sum += SingleCompare(compare.x, threshold);
            sum += SingleCompare(compare.y, threshold);
            sum += SingleCompare(compare.z, threshold);

            return sum;
        }

        private int SingleCompare(float compare, float threshold)
        {
            var sum = 0;
            if (compare < threshold)
            {
                sum += -1;
            }
            else if (compare > threshold)
            {
                sum += 1;
            }
            //else no change

            return sum;
        }
    }

    private EntityQuery m_MaterialQuery;

    public void OnCreate(ref SystemState state)
    {
        m_MaterialQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(ColliderMaterialsComponent)
            }
        });
    }

    public void OnUpdate(ref SystemState state)
    {
        var blobJob = new ChangeColliderBlobJob().Schedule(state.Dependency);
        blobJob.Complete();

        // Change the colour of the colliders based on their restitution (which was changed by the blob job)
        var entityArray = m_MaterialQuery.ToEntityArray(Allocator.Temp);
        if (entityArray.Length == 0) return;
        var materials = state.EntityManager.GetSharedComponentManaged<ColliderMaterialsComponent>(entityArray[0]);

        var renderMeshArraysToAdd = new List<RenderMeshArray>();
        var entitiesToAdd = new NativeList<Entity>(Allocator.Temp);
        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

        foreach (var(renderMeshArray, collider, blob, entity) in SystemAPI
                 .Query<RenderMeshArray, RefRO<PhysicsCollider>, ChangeColliderBlob>()
                 .WithEntityAccess()
                 .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities))
        {
            var restitution = collider.ValueRO.Value.Value.GetRestitution();
            var useMaterial = (restitution < k_GrowingRestitution) ? materials.ShrinkMaterial : materials.GrowMaterial;
            var materialArray = new[] { (UnityObjectRef<UnityEngine.Material>)useMaterial };
            var newRenderMeshArray = new RenderMeshArray(materialArray, renderMeshArray.MeshReferences);

            renderMeshArraysToAdd.Add(newRenderMeshArray);
            entitiesToAdd.Add(entity);
        }
        commandBuffer.Playback(state.EntityManager);

        for (int i = 0; i < entitiesToAdd.Length; i++)
        {
            var e = entitiesToAdd[i];
            var renderMeshArray = renderMeshArraysToAdd[i];

            RenderMeshUtility.AddComponents(
                e,
                state.EntityManager,
                new RenderMeshDescription(ShadowCastingMode.Off),
                renderMeshArray,
                MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0)
            );
        }

        entitiesToAdd.Dispose();
        commandBuffer.Dispose();
        entityArray.Dispose();
    }

    public void OnDestroy(ref SystemState state) {}
}
