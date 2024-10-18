using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class ColliderBakeTransformAuthoring : MonoBehaviour
{
    public Vector3 Translation;
    public Quaternion Rotation = Quaternion.identity;
    public Vector3 Scale = new(1, 1, 1);
    public Vector2 ShearXY;
    public Vector2 ShearXZ;
    public Vector2 ShearYZ;
    public float AnimationDuration = 0;
    public bool DriftPrevention = true;
    public float DriftErrorThreshold = 0.05f;

    public class Baker : Baker<ColliderBakeTransformAuthoring>
    {
        public override void Bake(ColliderBakeTransformAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new ColliderBakeTransform
            {
                Translation = authoring.Translation,
                Rotation = authoring.Rotation,
                Scale = authoring.Scale,
                ShearXY = authoring.ShearXY,
                ShearXZ = authoring.ShearXZ,
                ShearYZ = authoring.ShearYZ,
                AnimationDuration = authoring.AnimationDuration,
                DriftPrevention = authoring.DriftPrevention,
                DriftErrorThreshold = authoring.DriftErrorThreshold,
                FrameCount = 0
            });
        }
    }
}

public struct ColliderBakeTransform : IComponentData
{
    public float3 Translation;
    public quaternion Rotation;
    public float3 Scale;
    public float2 ShearXY;
    public float2 ShearXZ;
    public float2 ShearYZ;

    public float AnimationDuration;
    public bool DriftPrevention;
    public float DriftErrorThreshold;

    public int FrameCount;
    public BlobAssetReference<Unity.Physics.Collider> OriginalCollider;
    public PostTransformMatrix OriginalPostTransformMatrix;
}

public struct SaveColliderBlobForDisposal : ICleanupComponentData
{
    public BlobAssetReference<Unity.Physics.Collider> Collider;
}
