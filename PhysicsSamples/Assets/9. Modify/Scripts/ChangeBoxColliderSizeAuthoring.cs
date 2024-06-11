using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using BoxCollider = Unity.Physics.BoxCollider;

public struct ChangeBoxColliderSize : IComponentData
{
    public float3 Min;
    public float3 Max;
    public float3 Target;
}

// In general, you should treat colliders as immutable data at run-time, as several bodies might share the same collider.
// If you plan to modify mesh or convex colliders at run-time, remember to tick the Force Unique box on the PhysicsShapeAuthoring component.
// This guarantees that the PhysicsCollider component will have a unique instance in all cases.

public class ChangeBoxColliderSizeAuthoring : MonoBehaviour
{
    public float3 Min = 0;
    public float3 Max = 10;
}

class ChangeBoxColliderSizeBaker : Baker<ChangeBoxColliderSizeAuthoring>
{
    public override void Bake(ChangeBoxColliderSizeAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.ManualOverride);
        AddComponent(entity, new ChangeBoxColliderSize
        {
            Min = authoring.Min,
            Max = authoring.Max,
            Target = math.lerp(authoring.Min, authoring.Max, 0.5f),
        });

        // Add PostTransformMatrix component in case this isn't already done by the body baker,
        // which occurs if the collider world transform does not already have shear or non-uniform scale at edit time.
        // If shear or non-uniform scale do exist at edit time, the PostTransformMatrix component will necessarily be added
        // by the body baker.
        float4x4 localToWorld = authoring.gameObject.transform.localToWorldMatrix;
        if (!(localToWorld.HasShear() || localToWorld.HasNonUniformScale()))
        {
            AddComponent(entity, new PostTransformMatrix
            {
                Value = float4x4.identity,
            });
        }
    }
}

[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial struct ChangeBoxColliderSizeSystem : ISystem
{
    [BurstCompile]
    public partial struct ChangeBoxColliderSizeJob : IJobEntity
    {
        public void Execute(ref PhysicsCollider collider, ref ChangeBoxColliderSize size, ref PostTransformMatrix postTransformMatrix)
        {
            // make sure we are dealing with boxes
            if (collider.Value.Value.Type != ColliderType.Box) return;

            // tweak the physical representation of the box

            // NOTE: this approach affects all instances using the same BlobAsset
            // so you cannot simply use this approach for instantiated prefabs
            // if you want to modify prefab instances independently, you need to create
            // unique BlobAssets at run-time and dispose them when you are done

            float3 oldSize = 1.0f;
            float3 newSize = 1.0f;
            unsafe
            {
                // grab the box pointer
                BoxCollider* bxPtr = (BoxCollider*)collider.ColliderPtr;
                oldSize = bxPtr->Size;
                newSize = math.lerp(oldSize, size.Target, 0.05f);

                // if we have reached the target size, get a new target
                float3 newTargetSize = math.select(size.Min, size.Max, size.Target == size.Min);
                size.Target = math.select(size.Target, newTargetSize, math.abs(newSize - size.Target) < new float3(0.1f));

                var boxGeometry = bxPtr->Geometry;
                boxGeometry.Size = newSize;
                bxPtr->Geometry = boxGeometry;
            }

            // now tweak the graphical representation of the box
            float3 newScale = newSize / oldSize;
            postTransformMatrix.Value.c0 *= newScale.x;
            postTransformMatrix.Value.c1 *= newScale.y;
            postTransformMatrix.Value.c2 *= newScale.z;

            if (!collider.IsUnique)
            {
                throw new ArgumentException($"Error: The collider {collider.Value.Value} is not unique");
            }
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new ChangeBoxColliderSizeJob().Schedule(state.Dependency);
    }
}
