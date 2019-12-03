using Unity.Entities;
using Unity.Mathematics;

public struct CartesianGridOnPlane : IComponentData
{
    public BlobAssetReference<CartesianGridOnPlaneBlob> Blob;
}

public struct CartesianGridOnPlaneBlob
{
    public ushort RowCount;
    public ushort ColCount;
    
    // Offset vector for trailing edge of unit-size object.
    // Pre-added to grid center.
    //   [0] = ( cx +  0.0f, cz + -0.5f ); // North
    //   [1] = ( cx +  0.0f, cz +  0.5f ); // South
    //   [2] = ( cx +  0.5f, cz +  0.0f ); // West
    //   [3] = ( cx + -0.5f, cz +  0.0f ); // East
    public BlobArray<float2> TrailingOffsets;
    
    // NativeArray<4bit x rowCount x colCount> of walls.
    //     0x01 = North
    //     0x02 = South
    //     0x04 = West
    //     0x08 = East
    public BlobArray<byte> Walls;
}
