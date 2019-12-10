using System;
using Unity.Entities;

public struct CartesianGridOnCubeGenerator : IComponentData
{
    public BlobAssetReference<CartesianGridOnCubeGeneratorBlob> Blob;
}

public struct CartesianGridOnCubeGeneratorBlob
{
    public int RowCount;
    public Entity WallPrefab;
    public BlobArray<Entity> FloorPrefab;
    public float WallSProbability;
    public float WallWProbability;
}
