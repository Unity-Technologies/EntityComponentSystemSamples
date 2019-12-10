using System;
using Unity.Entities;
using Unity.Mathematics;

public struct MeshBBBlobAsset
{
    public float MeshScale;
    public float3 MinBoundingBox;
    public float3 MaxBoundingBox;
    public BlobArray<float3> Vertices;
}

public struct MeshBBComponent : IComponentData
{
    public BlobAssetReference<MeshBBBlobAsset> BlobData;

    public MeshBBComponent(BlobAssetReference<MeshBBBlobAsset> blob)
    {
        BlobData = blob;
    }
}