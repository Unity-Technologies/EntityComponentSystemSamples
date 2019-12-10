using System;
using Unity.Entities;

public struct CartesianGridOnPlaneGenerator : IComponentData
{
    public BlobAssetReference<CartesianGridOnPlaneGeneratorBlob> Blob;
}

public struct CartesianGridOnPlaneGeneratorBlob
{
    public int RowCount;
    public int ColCount;
    public Entity WallPrefab;
    public BlobArray<Entity> FloorPrefab;
    public float WallSProbability;
    public float WallWProbability;
}
