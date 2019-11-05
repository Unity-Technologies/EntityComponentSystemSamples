using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct GridCube : IComponentData
{
}

public struct GridPlane : IComponentData
{
}

public struct GridConfig : IComponentData
{
    public ushort RowCount;
    public ushort ColCount;
}

// Direction along grid axis
//     0 = N
//     1 = S
//     2 = W
//     3 = E
public struct GridDirection : IComponentData
{
    public byte Value; // 2 bits current direction
}

// Speed of movement in grid-space
// - 6:10 fixed point instead of 32bit float (for size)
public struct GridSpeed : IComponentData
{
    public ushort Value; 
}

// NativeArray<4bit x rowCount x colCount> of walls.
//     0x01 = North
//     0x02 = South
//     0x04 = West
//     0x08 = East
public struct GridWalls : IBufferElementData
{
    public byte Value;
}

// Which face of the GridCube 
//     0 = X+
//     1 = X-
//     2 = Y+
//     3 = Y-
//     4 = Z+
//     5 = Z-
[WriteGroup((typeof(LocalToWorld)))]
public struct GridFace : IComponentData
{
    public byte Value;
}

// Coordinates quantized to the grid
public struct GridPosition : IComponentData, IEquatable<GridPosition>
{
    public short x;
    public short y;
    
    public GridPosition(float2 pos, int rowCount, int colCount)
    {
        // Grid indices of Trailing edge 
        // - Always positive when on the grid.
        // - However, in the case of a cube, the translation may go off the grid when travelling around the corner.
        //   In this case, stretch the one-off grid element to encompass the whole area off the grid.
        //   e.g. if it's off to the left, grid x value is always -1.
        x = (short)math.clamp(((int)(pos.x + 1.0f)) - 1, -1, colCount);
        y = (short)math.clamp(((int)(pos.y + 1.0f)) - 1, -1, rowCount);
    }
    
    public bool Equals(GridPosition other)
    {
        return ((other.x == x) && (other.y == y));
    }
}

public struct FaceLocalToWorld : IBufferElementData
{
    public float4x4 Value;
}

public struct GridTrailingOffset : IBufferElementData
{
    public float2 Value;
}


