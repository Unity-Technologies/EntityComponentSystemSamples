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
    static readonly byte[] m_NextDirection =
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
    
    public static readonly byte[] ReverseDirection =
    {
        1, 0, 3, 2 // N=S, S=N, W=E, E=W
    };
    
    // Rotate through variations of shortest paths.
    // - Note: Biased on first available if there are three available. But meh.
    public static readonly byte[] PathVariation =
    {
        // -  N  S  NS W  NW SW NSW E  NE SE NSE WE NWE SWE NSWE
        0xff, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
        0xff, 0, 1, 1, 2, 2, 2, 1, 3, 3, 3, 1, 3, 2, 2, 1,
        0xff, 0, 1, 0, 2, 0, 1, 2, 3, 0, 1, 3, 2, 3, 3, 2,
        0xff, 0, 1, 1, 2, 2, 2, 0, 3, 3, 3, 0, 3, 0, 1, 3,
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

    /// <summary>
    /// Snap translation to center of grid cell along the direction movement.
    /// This ensures translation does not drift substantially over time with accumulated errors.
    /// </summary>
    /// <param name="translation">Translation (in grid space) to snap</param>
    /// <param name="dir">Direction of movement. See: CartesianGridDirection</param>
    /// <param name="cellPosition">Current cell to snap to</param>
    /// <param name="cellCenterOlffset">Offset relative to center of grid.</param>
    /// <returns></returns>
    public static float3 SnapToGridAlongDirection(float3 translation, byte dir, CartesianGridCoordinates cellPosition, float2 cellCenterOffset)
    {
        // When (dir == N,S) clamp to grid cell center x
        // When (dir == W,E) clamp to grid cell center y
        var mx = (dir >> 1) * 1.0f;
        var my = ((dir >> 1) ^ 1) * 1.0f;

        return new float3
        {
            x = (mx * translation.x) + (my * (cellPosition.x - cellCenterOffset.x)),
            z = (my * translation.z) + (mx * (cellPosition.y - cellCenterOffset.y)),
            y = translation.y
        };
    }

    static byte LookupWalls(CartesianGridCoordinates cartesianGridCoordinates, int rowCount, int colCount, byte* gridWalls)
    {
        var rowStride = ((colCount + 1) / 2);
        var gridWallsIndex = (cartesianGridCoordinates.y * rowStride) + (cartesianGridCoordinates.x / 2);

        // Walls in current grid element (odd columns in upper 4 bits of byte)
        var walls = (gridWalls[gridWallsIndex] >> ((cartesianGridCoordinates.x & 1) * 4)) & 0x0f;

        return (byte)walls;
    }

    /// <summary>
    /// Pick valid direction to "bounce" off (move along) walls. 
    /// </summary>
    /// <param name="cellPosition">Position to test (must be valid on grid)</param>
    /// <param name="dir">Current movement direction</param>
    /// <param name="rowCount">Height of grid</param>
    /// <param name="colCount">Width of grid</param>
    /// <param name="gridWalls">Table representing walls of grid.</param>
    /// <param name="pathIndex">Index to select from multiple valid options. See: m_NextDirection</param>
    /// <returns></returns>
    // gridPosition needs to be on-grid (positive and < [colCount, rowCount]) when looking up next direction.
    public static byte BounceDirectionOffWalls(CartesianGridCoordinates cellPosition, byte dir, int rowCount, int colCount, byte* gridWalls, int pathIndex)
    {
        var pathOffset = pathIndex * 16;
        var walls = LookupWalls(cellPosition, rowCount, colCount, gridWalls);
        
        // New direction = f( grid index, movement direction )
        return (byte)((m_NextDirection[pathOffset + walls] >> (dir * 2)) & 0x03);
    }

    /// <summary>
    /// Return directions from cellPosition which are not blocked by walls. 
    /// </summary>
    /// <param name="cellPosition">Position to test</param>
    /// <param name="rowCount">Height of grid</param>
    /// <param name="colCount">Width of grid</param>
    /// <param name="gridWalls">Table representing walls of grid.</param>
    /// <returns></returns>
    public static byte ValidDirections(CartesianGridCoordinates cellPosition, int rowCount, int colCount, byte* gridWalls)
    {
        return (byte)(0x0f & ~LookupWalls(cellPosition, rowCount, colCount, gridWalls));
    }

    /// <summary>
    /// Which edge of GridCube face is being exited (if any)
    /// </summary>
    /// <param name="cellPosition">Position to test</param>
    /// <param name="rowCount">Height/Width of cube face</param>
    /// <returns></returns>
    public static int CubeExitEdge(CartesianGridCoordinates cellPosition, int rowCount)
    {
        // Which edge of GridCube face is being exited (if any)
        var edge = -1;

        // Edge is in order specified in m_NextFaceIndex and m_NextFaceDirection 
        // - Matches GridDirection values.
        edge = math.select(edge, 0, cellPosition.y >= rowCount);
        edge = math.select(edge, 1, cellPosition.y < 0);
        edge = math.select(edge, 2, cellPosition.x < 0);
        edge = math.select(edge, 3, cellPosition.x >= rowCount);

        return edge;
    }
}