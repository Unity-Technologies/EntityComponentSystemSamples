using Unity.Entities;
using Unity.Mathematics;

namespace Advanced.BlobAssets
{
    public struct MeshBBBlobAsset
    {
        public float3 MinBoundingBox;
        public float3 MaxBoundingBox;
    }

    public struct MeshBBComponent : IComponentData
    {
        public BlobAssetReference<MeshBBBlobAsset> BlobData;
    }

    [BakingType]
    public struct RawMeshComponent : IComponentData
    {
        public float MeshScale;
        public Hash128 Hash;
    }

    [BakingType]
    public struct MeshVertex : IBufferElementData
    {
        public float3 Value;
    }

    public struct CleanupComponent : ICleanupComponentData
    {
        public Hash128 Hash;
    }
}
