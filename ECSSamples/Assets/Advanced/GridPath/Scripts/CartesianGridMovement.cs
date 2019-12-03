using System;
using Unity.Mathematics;

public static unsafe class CartesianGridMovement
{
    // Convenience vectors for turning direction.
    public static readonly float[] UnitMovement =
    {
        0.0f, 1.0f, // North
        0.0f, -1.0f, // South
        -1.0f, 0.0f, // West
        1.0f, 0.0f, // East
    };

    // Next Direction lookup by grid element walls
    //   - Calculate gridX, gridY based on current actual position (Translation)
    //   - Get 4 path options = [(gridY * rowCount)+gridX]
    //   - Select path option based on current direction (Each of 4 direction 2bits of result)
    public static readonly byte[] NextDirection =
    {
        // Standard paths. Bounce off walls.
        // Two paths because two directions can be equally likely.

        // PathSet[0]
        0xe4, 0xe7, 0xec, 0xef, 0xc4, 0xd7, 0xcc, 0xff,
        0x24, 0x66, 0x28, 0xaa, 0x04, 0x55, 0x00, 0xe4,

        // PathSet[1]
        0xe4, 0xe6, 0xe8, 0xea, 0xd4, 0xd7, 0xcc, 0xff,
        0x64, 0x66, 0x28, 0xaa, 0x54, 0x55, 0x00, 0xe4,

        // Path (rare) variations below    
        // Very occasionally, move without bouncing off wall.

        // Assume north wall
        0xe6, 0xe6, 0xea, 0xea, 0xd7, 0xd7, 0xff, 0xff,
        0x66, 0x66, 0xaa, 0xaa, 0x55, 0x55, 0x00, 0xe4,

        // Assume south wall
        0xe8, 0xea, 0xe8, 0xea, 0xcc, 0xff, 0xcc, 0xff,
        0x28, 0xaa, 0x28, 0xaa, 0x00, 0x55, 0x00, 0xe4,

        // Assume west wall
        0xd4, 0xd7, 0xcc, 0xff, 0xd4, 0xd7, 0xcc, 0xff,
        0x54, 0x55, 0x00, 0xaa, 0x54, 0x55, 0x00, 0xe4,

        // Assume east wall
        0x64, 0x66, 0x28, 0xaa, 0x54, 0x55, 0x00, 0xff,
        0x64, 0x66, 0x28, 0xaa, 0x54, 0x55, 0x00, 0xe4,
    };

    public static int NextPathIndex(ref int pathCounter)
    {
        var isRandomVariation = (pathCounter & 0x0f) == 0;
        var nextPathIndex = 0;
        
        // Once every 16 frames, select an arbitrary variation (if happen to be crossing threshold)
        // Both crossing a threshold and hitting a variation at the same time is a very rare event.
        // The purpose of these variations is to occasionally kick things out that are caught in a
        // movement loop.
        if (isRandomVariation)
        {
            nextPathIndex = 2+(pathCounter >> 4);
        }

        // Otherwise select one of two global variations of paths divided by 0.5 probability.
        else
        {
            nextPathIndex = pathCounter & 1;
        }

        
        pathCounter = (pathCounter + 1) & 0x003f;
        return nextPathIndex;
    }

    public static float3 ClampToGrid(float3 v, byte dir, CartesianGridCoordinates cartesianGridCoordinates, float2 cellCenterOffset)
    {
        // When (dir == N,S) clamp to grid cell center x
        // When (dir == W,E) clamp to grid cell center y
        var mx = (dir >> 1) * 1.0f;
        var my = ((dir >> 1) ^ 1) * 1.0f;

        return new float3
        {
            x = (mx * v.x) + (my * (cartesianGridCoordinates.x - cellCenterOffset.x)),
            z = (my * v.z) + (mx * (cartesianGridCoordinates.y - cellCenterOffset.y)),
            y = v.y
        };
    }

    // gridPosition needs to be on-grid (positive and < [colCount, rowCount]) when looking up next direction.
    public static byte LookupGridDirectionFromWalls(CartesianGridCoordinates cartesianGridCoordinates, byte dir, int rowStride, byte* gridWalls, int pathIndex)
    {
        var pathOffset = pathIndex * 16;

        // Index into grid array
        var gridWallsIndex = (cartesianGridCoordinates.y * rowStride) + (cartesianGridCoordinates.x / 2);

        // Walls in current grid element (odd columns in upper 4 bits of byte)
        var walls = (gridWalls[gridWallsIndex] >> ((cartesianGridCoordinates.x & 1) * 4)) & 0x0f;

        // New direction = f( grid index, movement direction )
        return (byte)((NextDirection[pathOffset + walls] >> (dir * 2)) & 0x03);
    }
}