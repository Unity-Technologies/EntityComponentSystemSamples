using System;
using Unity.Entities;
using Unity.Mathematics;

// Direction along grid axis
//     0 = N
//     1 = S
//     2 = W
//     3 = E
public struct CartesianGridDirection : IComponentData
{
    public byte Value; // 2 bits current direction
}

// Speed of movement in grid-space
// - 6:10 fixed point instead of 32bit float (for size)
public struct CartesianGridSpeed : IComponentData
{
    public ushort Value; 
}

// Coordinates quantized to the grid
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
}