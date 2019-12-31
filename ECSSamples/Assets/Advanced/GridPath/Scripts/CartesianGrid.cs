using System;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Available directions along grid axis
/// </summary>
[Flags]
public enum CartesianGridDirectionBit : byte
{
    None  = 0x00,
    North = 0x01,
    South = 0x02,
    West  = 0x04,
    East  = 0x08
}

/// <summary>
/// Direction along grid axis
///     0 = N
///     1 = S
///     2 = W
///     3 = E
///  0xff = No movement
/// </summary>
public struct CartesianGridDirection : IComponentData
{
    public byte Value; // 2 bits current direction
}

/// <summary>
/// Speed of movement in grid-space
/// - 6:10 fixed point instead of 32bit float (for size)
/// </summary>
public struct CartesianGridSpeed : IComponentData
{
    public ushort Value; 
}


/// <summary>
/// Coordinates quantized to the grid
/// </summary>
public struct CartesianGridCoordinates : IComponentData, IEquatable<CartesianGridCoordinates>
{
    public short x;
    public short y;
    
    public CartesianGridCoordinates(float2 pos, int rowCount, int colCount)
    {
        // Quantized grid coordinates from position in grid space
        // - Always positive when on the grid. (Up, Right)
        // - Basically just float to int cast.
        // - However, in the case of a cube, the translation may go off the grid when travelling around the corner.
        //   In this case, stretch the one-off grid element to encompass the whole area off the grid.
        //   e.g. if it's off to the left, grid x value is always -1.
        x = (short)math.clamp(((int)(pos.x + 1.0f)) - 1, -1, colCount);
        y = (short)math.clamp(((int)(pos.y + 1.0f)) - 1, -1, rowCount);
    }
    
    public bool Equals(CartesianGridCoordinates other)
    {
        return ((other.x == x) && (other.y == y));
    }

    public bool OnGrid(int rowCount, int colCount) =>
        ((x >= 0) &&
            (x <= (colCount - 1)) &&
            (y >= 0) &&
            (y <= (rowCount - 1)));
}

/// <summary>
/// Is a target on the map, so will be tracked by CartesianGridFollowTarget
/// </summary>
public struct CartesianGridTarget : IComponentData
{
}

/// <summary>
/// Follow nearest CartesianGridTarget 
/// </summary>
[WriteGroup(typeof(CartesianGridDirection))]
public struct CartesianGridFollowTarget : IComponentData
{
}

/// <summary>
/// Table of shortest paths to target for every grid cell on map.
/// </summary>
public struct CartesianGridTargetDirection : IBufferElementData
{
    byte m_Value; // Never accessed directly.
}

/// <summary>
/// Table of shortest distance along shortest path to target for every grid cell on map.
/// </summary>
public struct CartesianGridTargetDistance : IBufferElementData
{
    int m_Value; // Never accessed directly.
}

/// <summary>
/// Track last known grid coordinate of target
/// </summary>
public struct CartesianGridTargetCoordinates : IComponentData, IEquatable<CartesianGridCoordinates>
{
    public short x;
    public short y;

    public CartesianGridTargetCoordinates(CartesianGridCoordinates other)
    {
        x = other.x;
        y = other.y;
    }
    
    public bool Equals(CartesianGridCoordinates other)
    {
        return ((other.x == x) && (other.y == y));
    } 
}