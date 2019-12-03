using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct CartesianGridOnCube : IComponentData
{
    public BlobAssetReference<CartesianGridOnCubeBlob> Blob;
}

public struct CartesianGridOnCubeBlob
{
    public ushort RowCount;
    
    // Offset vector for trailing edge of unit-size object.
    // Pre-added to grid center.
    //   [0] = ( cx +  0.0f, cz + -0.5f ); // North
    //   [1] = ( cx +  0.0f, cz +  0.5f ); // South
    //   [2] = ( cx +  0.5f, cz +  0.0f ); // West
    //   [3] = ( cx + -0.5f, cz +  0.0f ); // East
    public BlobArray<float2> TrailingOffsets;
    
    // [4bit x rowCount x colCount] of walls.
    //     0x01 = North
    //     0x02 = South
    //     0x04 = West
    //     0x08 = East
    public BlobArray<byte> Walls;

    // For each face[6], local to world transform. (Order as in CubeFace)
    public BlobArray<float4x4> FaceLocalToWorld;
    
    // For each face[6], world to local transform. (Order as in CubeFace)
    public BlobArray<float4x4> FaceWorldToLocal;
    
    // For each face by each face [6*6], local to local transform. (Order as in CubeFace)
    public BlobArray<float4x4> FaceLocalToLocal;
}

// Which face of the Cube
//     0 = X+
//     1 = X-
//     2 = Y+
//     3 = Y-
//     4 = Z+
//     5 = Z-
[WriteGroup((typeof(LocalToWorld)))]
public struct CubeFace : IComponentData
{
    public byte Value;
}

