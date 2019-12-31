using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

public static unsafe class CartesianGridOnPlaneShortestPath
{
    // Simple grassfire to calculate shortest distance along path to target position for every position.
    // - i.e. breadth-first expansion from target position.
    static void CalculateShortestWalkableDistancesToTarget(int rowCount, int colCount, byte* gridWalls, CartesianGridCoordinates targetPosition, NativeArray<int> targetDistances)
    {
        var cellCount = rowCount * colCount;
        var closed = new UnsafeBitArray(cellCount, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var pending = new UnsafeBitArray(cellCount, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var open = new UnsafeRingQueue<int>(cellCount, Allocator.Temp);

        var targetCellIndex = (targetPosition.y * colCount) + targetPosition.x;
        var cellIndexNorth = targetCellIndex + colCount;
        var cellIndexSouth = targetCellIndex - colCount;
        var cellIndexWest = targetCellIndex - 1;
        var cellIndexEast = targetCellIndex + 1;

        for (int i = 0; i < targetDistances.Length; i++)
            targetDistances[i] = -1;
        targetDistances[targetCellIndex] = 0;

        pending.Set(targetCellIndex, true);
        closed.Set(targetCellIndex, true);

        var validDirections = CartesianGridMovement.ValidDirections(targetPosition, rowCount, colCount, gridWalls);
        var validNorth = ((validDirections & (byte)CartesianGridDirectionBit.North) != 0);
        var validSouth = ((validDirections & (byte)CartesianGridDirectionBit.South) != 0);
        var validWest = ((validDirections & (byte)CartesianGridDirectionBit.West) != 0);
        var validEast = ((validDirections & (byte)CartesianGridDirectionBit.East) != 0);

        if (validNorth)
        {
            open.Enqueue(cellIndexNorth);
            pending.Set(cellIndexNorth, true);
        }

        if (validSouth)
        {
            open.Enqueue(cellIndexSouth);
            pending.Set(cellIndexSouth, true);
        }

        if (validWest)
        {
            open.Enqueue(cellIndexWest);
            pending.Set(cellIndexWest, true);
        }

        if (validEast)
        {
            open.Enqueue(cellIndexEast);
            pending.Set(cellIndexEast, true);
        }

        CalculateShortestWalkableDistancesToTargetInner(rowCount, colCount, gridWalls, targetDistances, pending, closed, open);

        closed.Dispose();
        pending.Dispose();
        open.Dispose();
    }

    static void CalculateShortestWalkableDistancesToTargetInner(int rowCount, int colCount, byte* gridWalls, NativeArray<int> targetDistances, UnsafeBitArray pending, UnsafeBitArray closed, UnsafeRingQueue<int> open)
    {
        var cellCount = rowCount * colCount;

        while (open.Count > 0)
        {
            var cellIndex = open.Dequeue();
            var y = cellIndex / colCount;
            var x = cellIndex - (y * colCount);
            var cellPosition = new CartesianGridCoordinates { x = (short)x, y = (short)y };

            var validDirections = CartesianGridMovement.ValidDirections(cellPosition, rowCount, colCount, gridWalls);
            var validNorth = ((validDirections & (byte)CartesianGridDirectionBit.North) != 0);
            var validSouth = ((validDirections & (byte)CartesianGridDirectionBit.South) != 0);
            var validWest = ((validDirections & (byte)CartesianGridDirectionBit.West) != 0);
            var validEast = ((validDirections & (byte)CartesianGridDirectionBit.East) != 0);

            var cellIndexNorth = cellIndex + colCount;
            var cellIndexSouth = cellIndex - colCount;
            var cellIndexWest = cellIndex - 1;
            var cellIndexEast = cellIndex + 1;

            var distanceNorth = cellCount + 1;
            var distanceSouth = cellCount + 1;
            var distanceEast = cellCount + 1;
            var distanceWest = cellCount + 1;

            if (validNorth)
            {
                if (closed.IsSet(cellIndexNorth))
                {
                    distanceNorth = targetDistances[cellIndexNorth];
                }
                else if (!pending.IsSet(cellIndexNorth))
                {
                    open.Enqueue(cellIndexNorth);
                    pending.Set(cellIndexNorth, true);
                }
            }

            if (validSouth)
            {
                if (closed.IsSet(cellIndexSouth))
                {
                    distanceSouth = targetDistances[cellIndexSouth];
                }
                else if (!pending.IsSet(cellIndexSouth))
                {
                    open.Enqueue(cellIndexSouth);
                    pending.Set(cellIndexSouth, true);
                }
            }

            if (validWest)
            {
                if (closed.IsSet(cellIndexWest))
                {
                    distanceWest = targetDistances[cellIndexWest];
                }
                else if (!pending.IsSet(cellIndexWest))
                {
                    open.Enqueue(cellIndexWest);
                    pending.Set(cellIndexWest, true);
                }
            }

            if (validEast)
            {
                if (closed.IsSet(cellIndexEast))
                {
                    distanceEast = targetDistances[cellIndexEast];
                }
                else if (!pending.IsSet(cellIndexEast))
                {
                    open.Enqueue(cellIndexEast);
                    pending.Set(cellIndexEast, true);
                }
            }

            var bestDist = math.cmin(new int4(distanceNorth, distanceSouth, distanceEast, distanceWest)) + 1;

            targetDistances[cellIndex] = bestDist;
            closed.Set(cellIndex, true);
        }
    }

    // Sample valid neighboring distances from point.
    // - Smallest distance less than current position's distance is best next path to target.
    // - May result in more than one best direction (any of NSWE)
    // - May result in no best direction if on island (result=0xff)
    static void CalculateShortestPathGivenDistancesToTarget(NativeArray<byte> targetDirections, int rowCount, int colCount, NativeArray<int> cellDistances, byte* gridWalls)
    {
        var cellCount = rowCount * colCount;
        var rowStride = ((colCount + 1) / 2);

        for (var i = 0; i < (rowStride*rowCount); i++)
            targetDirections[i] = 0;

        for (var cellIndex = 0; cellIndex < cellCount; cellIndex++)
        {
            var y = cellIndex / colCount;
            var x = cellIndex - (y * colCount);
            var cellPosition = new CartesianGridCoordinates { x = (short)x, y = (short)y };

            var validDirections = CartesianGridMovement.ValidDirections(cellPosition, rowCount, colCount, gridWalls);
            var validNorth = ((validDirections & (byte)CartesianGridDirectionBit.North) != 0);
            var validSouth = ((validDirections & (byte)CartesianGridDirectionBit.South) != 0);
            var validWest = ((validDirections & (byte)CartesianGridDirectionBit.West) != 0);
            var validEast = ((validDirections & (byte)CartesianGridDirectionBit.East) != 0);

            var cellIndexNorth = cellIndex + colCount;
            var cellIndexSouth = cellIndex - colCount;
            var cellIndexWest = cellIndex - 1;
            var cellIndexEast = cellIndex + 1;

            var distanceNorth = cellCount + 1;
            var distanceSouth = cellCount + 1;
            var distanceEast = cellCount + 1;
            var distanceWest = cellCount + 1;

            if (validNorth)
                distanceNorth = cellDistances[cellIndexNorth];
            if (validSouth)
                distanceSouth = cellDistances[cellIndexSouth];
            if (validWest)
                distanceWest = cellDistances[cellIndexWest];
            if (validEast)
                distanceEast = cellDistances[cellIndexEast];

            var bestDist = math.cmin(new int4(distanceNorth, distanceSouth, distanceEast, distanceWest));
            var dist = cellDistances[cellIndex];

            if ((bestDist < dist) && (bestDist <= cellCount))
            {
                var bestDir = 0;
                if (distanceNorth == bestDist)
                    bestDir |= (byte)CartesianGridDirectionBit.North;
                if (distanceSouth == bestDist)
                    bestDir |= (byte)CartesianGridDirectionBit.South;
                if (distanceWest == bestDist)
                    bestDir |= (byte)CartesianGridDirectionBit.West;
                if (distanceEast == bestDist)
                    bestDir |= (byte)CartesianGridDirectionBit.East;

                var targetDirectionsIndex = (y * rowStride) + (x / 2);

                targetDirections[targetDirectionsIndex] |= (byte)(bestDir << (4 * (x & 1)));
            }
        }
    }

    /// <summary>
    /// Find shortest path for every position on grid to target position
    /// </summary>
    /// <param name="targetDirections">Result table of all shortest paths for every position on grid to targetPosition</param>
    /// <param name="rowCount">Height of grid</param>
    /// <param name="colCount">Width of grid</param>
    /// <param name="targetPosition">Generate all shortest paths to this position.</param>
    /// <param name="gridWalls">Table representing walls/obstacles in grid. See: CartesianGridMovement</param>
    public static void CalculateShortestPathsToTarget(NativeArray<byte> targetDirections, int rowCount, int colCount, CartesianGridCoordinates targetPosition, byte* gridWalls)
    {
        var targetDistances = new NativeArray<int>(rowCount * colCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

        CalculateShortestWalkableDistancesToTarget(rowCount, colCount, gridWalls, targetPosition, targetDistances);
        CalculateShortestPathGivenDistancesToTarget(targetDirections, rowCount, colCount, targetDistances, gridWalls);

        targetDistances.Dispose();
    }

    /// <summary>
    /// Find directions along shortest path(s) to target.
    /// </summary>
    /// <param name="cellPosition">Current position</param>
    /// <param name="rowCount">Height of grid</param>
    /// <param name="colCount">Width of grid</param>
    /// <param name="targetDirections">Lookup table generated by CalculatePathsToTarget</param>
    /// <returns>Any (or none) best directions along any shortest path in form of CartesianGridDirectionBit</returns>
    public static byte LookupDirectionToTarget(CartesianGridCoordinates cellPosition, int rowCount, int colCount, NativeArray<byte> targetDirections)
    {
        var rowStride = ((colCount + 1) / 2);
        var gridWallsIndex = (cellPosition.y * rowStride) + (cellPosition.x / 2);

        // Walls in current grid element (odd columns in upper 4 bits of byte)
        var directions = (targetDirections[gridWallsIndex] >> ((cellPosition.x & 1) * 4)) & 0x0f;
        return (byte)directions;
    }
}
