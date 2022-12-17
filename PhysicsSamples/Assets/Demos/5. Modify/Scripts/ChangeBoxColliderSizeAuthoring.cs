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

#if !ENABLE_TRANSFORM_V1
        AddComponent(entity, new PostTransformScale
        {
            Value = float3x3.identity,
        });
#else
        AddComponent(entity, new NonUniformScale
        {
            Value = new float3(1)
        });
#endif
    }
}

[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
[BurstCompile]
public partial struct ChangeBoxColliderSizeSystem : ISystem
{
    [BurstCompile]
    public partial struct ChangeBoxColliderSizeJob : IJobEntity
    {
#if !ENABLE_TRANSFORM_V1
        public void Execute(ref PhysicsCollider collider, ref ChangeBoxColliderSize size, ref PostTransformScale postTransformScale)
#else
        public void Execute(ref PhysicsCollider collider, ref ChangeBoxColliderSize size, ref NonUniformScale scale)
#endif
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
#if !ENABLE_TRANSFORM_V1
            // now tweak the graphical representation of the box
            float3x3 oldScale = postTransformScale.Value;

            float3 newScale = newSize / oldSize;
            postTransformScale.Value.c0 *= newScale.x;
            postTransformScale.Value.c1 *= newScale.y;
            postTransformScale.Value.c2 *= newScale.z;
#else
            // now tweak the graphical representation of the box
            float3 oldScale = scale.Value;
            float3 newScale = oldScale;

            newScale *= newSize / oldSize;
            scale.Value = newScale;
#endif
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new ChangeBoxColliderSizeJob().Schedule(state.Dependency);
    }
}
