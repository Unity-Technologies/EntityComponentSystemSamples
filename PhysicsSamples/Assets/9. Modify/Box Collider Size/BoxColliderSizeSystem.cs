using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Modify
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial struct BoxColliderSizeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ChangeBoxColliderSizeJob().Schedule();
        }

        [BurstCompile]
        public partial struct ChangeBoxColliderSizeJob : IJobEntity
        {
            public void Execute(ref PhysicsCollider collider, ref ChangeBoxColliderSize size,
                ref PostTransformMatrix postTransformMatrix)
            {
                // make sure we are dealing with boxes
                if (collider.Value.Value.Type != ColliderType.Box)
                {
                    return;
                }

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
                    size.Target = math.select(size.Target, newTargetSize,
                        math.abs(newSize - size.Target) < new float3(0.1f));

                    var boxGeometry = bxPtr->Geometry;
                    boxGeometry.Size = newSize;
                    bxPtr->Geometry = boxGeometry;
                }

                // now tweak the graphical representation of the box
                float3 newScale = newSize / oldSize;
                postTransformMatrix.Value.c0 *= newScale.x;
                postTransformMatrix.Value.c1 *= newScale.y;
                postTransformMatrix.Value.c2 *= newScale.z;
            }
        }
    }
}
