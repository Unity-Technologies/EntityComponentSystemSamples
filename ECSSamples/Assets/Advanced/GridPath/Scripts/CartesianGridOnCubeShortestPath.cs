using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;

public static unsafe class CartesianGridOnCubeShortestPath
{
    // Simple grassfire to calculate shortest distance along path to target position for every position.
    // - i.e. breadth-first expansion from target position.
    static void CalculateShortestWalkableDistancesToTarget(NativeArray<int> targetDistances, int rowCount, byte* gridWalls, CartesianGridCoordinates targetPosition, CartesianGridOnCubeFace cubeFace, float4x4* faceLocalToLocal)
    {
        var cellCount = rowCount * rowCount;
        var closed = new UnsafeBitArray(6*cellCount, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var pending = new UnsafeBitArray(6*cellCount, Allocator.Temp, NativeArrayOptions.ClearMemory);
        var open = new UnsafeRingQueue<int>(6*cellCount, Allocator.Temp);

        var faceIndex = cubeFace.Value;
        var faceTargetCellIndex = (targetPosition.y * rowCount) + targetPosition.x;

        for (int i = 0; i < targetDistances.Length; i++)
            targetDistances[i] = -1;

        var targetCellIndex = (faceIndex * cellCount) + faceTargetCellIndex;
        targetDistances[targetCellIndex] = 0;

        pending.Set(targetCellIndex, true);
        closed.Set(targetCellIndex, true);

        var cellIndexNorth = CartesianGridOnCubeUtility.CellIndexNorth(targetCellIndex, rowCount, faceLocalToLocal);
        var cellIndexSouth = CartesianGridOnCubeUtility.CellIndexSouth(targetCellIndex, rowCount, faceLocalToLocal);
        var cellIndexWest = CartesianGridOnCubeUtility.CellIndexWest(targetCellIndex, rowCount, faceLocalToLocal);
        var cellIndexEast = CartesianGridOnCubeUtility.CellIndexEast(targetCellIndex, rowCount, faceLocalToLocal);

        var rowStride = (rowCount + 1) / 2;
        var faceStride = rowCount * rowStride;
        var faceGridWallsOffset = faceIndex * faceStride;

        var validDirections = CartesianGridMovement.ValidDirections(targetPosition, rowCount, rowCount, gridWalls + faceGridWallsOffset);
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

        CalculateShortestWalkableDistancesToTargetInner(targetDistances, rowCount, gridWalls, faceLocalToLocal, pending, closed, open);

        closed.Dispose();
        pending.Dispose();
        open.Dispose();
    }

    static void CalculateShortestWalkableDistancesToTargetInner(NativeArray<int> targetDistances, int rowCount, byte* gridWalls, float4x4* faceLocalToLocal, UnsafeBitArray pending, UnsafeBitArray closed, UnsafeRingQueue<int> open)
    {
        var cellCount = rowCount * rowCount;
        var maxPathLength = 6*(cellCount + 1);

        while (open.Count > 0)
        {
            var cellIndex = open.Dequeue();
            var cellPosition = CartesianGridOnCubeUtility.CellFaceCoordinates(cellIndex, rowCount);
            var faceIndex = CartesianGridOnCubeUtility.CellFaceIndex(cellIndex, rowCount);

            var rowStride = (rowCount + 1) / 2;
            var faceStride = rowCount * rowStride;
            var faceGridWallsOffset = faceIndex * faceStride;

            var validDirections = CartesianGridMovement.ValidDirections(cellPosition, rowCount, rowCount, gridWalls + faceGridWallsOffset);
            var validNorth = ((validDirections & (byte)CartesianGridDirectionBit.North) != 0);
            var validSouth = ((validDirections & (byte)CartesianGridDirectionBit.South) != 0);
            var validWest = ((validDirections & (byte)CartesianGridDirectionBit.West) != 0);
            var validEast = ((validDirections & (byte)CartesianGridDirectionBit.East) != 0);

            var cellIndexNorth = CartesianGridOnCubeUtility.CellIndexNorth(cellIndex, rowCount, faceLocalToLocal);
            var cellIndexSouth = CartesianGridOnCubeUtility.CellIndexSouth(cellIndex, rowCount, faceLocalToLocal);
            var cellIndexWest = CartesianGridOnCubeUtility.CellIndexWest(cellIndex, rowCount, faceLocalToLocal);
            var cellIndexEast = CartesianGridOnCubeUtility.CellIndexEast(cellIndex, rowCount, faceLocalToLocal);

            var distanceNorth = maxPathLength;
            var distanceSouth = maxPathLength;
            var distanceEast = maxPathLength;
            var distanceWest = maxPathLength;

            var closedNorth = false;
            var closedSouth = false;
            var closedWest = false;
            var closedEast = false;

            if (validNorth)
            {
                if (closed.IsSet(cellIndexNorth))
                {
                    distanceNorth = targetDistances[cellIndexNorth];
                    closedNorth = true;
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
                    closedSouth = true;
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
                    closedWest = true;
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
                    closedEast = true;
                }
                else if (!pending.IsSet(cellIndexEast))
                {
                    open.Enqueue(cellIndexEast);
                    pending.Set(cellIndexEast, true);
                }
            }

            var closedNeighbor = closedNorth || closedSouth || closedWest || closedEast;
            Assert.IsTrue(closedNeighbor);

            var bestDist = math.cmin(new int4(distanceNorth, distanceSouth, distanceEast, distanceWest)) + 1;
            Assert.IsFalse(bestDist > (maxPathLength + 1));

            targetDistances[cellIndex] = bestDist;
            closed.Set(cellIndex, true);
        }
    }

    // Sample valid neighboring distances from point.
    // - Smallest distance less than current position's distance is best next path to target.
    // - May result in more than one best direction (any of NSWE)
    // - May result in no best direction if on island (result=0xff)
    static void CalculateShortestPathGivenDistancesToTarget(NativeArray<byte> targetDirections, int rowCount, NativeArray<int> cellDistances, byte* gridWalls, float4x4* faceLocalToLocal)
    {
        var cellCount = rowCount * rowCount;
        var maxPathLength = 6*(cellCount + 1);

        for (int i = 0; i < targetDirections.Length; i++)
            targetDirections[i] = 0;

        for (var cellIndex = 0; cellIndex < (6*cellCount); cellIndex++)
        {
            var cellPosition = CartesianGridOnCubeUtility.CellFaceCoordinates(cellIndex, rowCount);
            var faceIndex = CartesianGridOnCubeUtility.CellFaceIndex(cellIndex, rowCount);

            var rowStride = (rowCount + 1) / 2;
            var faceStride = rowCount * rowStride;
            var faceGridWallsOffset = faceIndex * faceStride;

            var validDirections = CartesianGridMovement.ValidDirections(cellPosition, rowCount, rowCount, gridWalls + faceGridWallsOffset);
            var validNorth = ((validDirections & (byte)CartesianGridDirectionBit.North) != 0);
            var validSouth = ((validDirections & (byte)CartesianGridDirectionBit.South) != 0);
            var validWest = ((validDirections & (byte)CartesianGridDirectionBit.West) != 0);
            var validEast = ((validDirections & (byte)CartesianGridDirectionBit.East) != 0);

            var cellIndexNorth = CartesianGridOnCubeUtility.CellIndexNorth(cellIndex, rowCount, faceLocalToLocal);
            var cellIndexSouth = CartesianGridOnCubeUtility.CellIndexSouth(cellIndex, rowCount, faceLocalToLocal);
            var cellIndexWest = CartesianGridOnCubeUtility.CellIndexWest(cellIndex, rowCount, faceLocalToLocal);
            var cellIndexEast = CartesianGridOnCubeUtility.CellIndexEast(cellIndex, rowCount, faceLocalToLocal);

            var distanceNorth = maxPathLength;
            var distanceSouth = maxPathLength;
            var distanceEast = maxPathLength;
            var distanceWest = maxPathLength;

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

            if ((bestDist < dist) && (bestDist < maxPathLength))
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

                var targetDirectionsIndex = (faceIndex * faceStride) + (cellPosition.y * rowStride) + (cellPosition.x / 2);

                targetDirections[targetDirectionsIndex] |= (byte)(bestDir << (4 * (cellPosition.x & 1)));
            }
        }
    }

    /// <summary>
    /// Find shortest path for every position on grid to target position
    /// </summary>
    /// <param name="targetDirections">Result table of all shortest paths for every position on grid to targetPosition</param>
    /// <param name="rowCount">Height of grid</param>
    /// <param name="targetPosition">Generate all shortest paths to this position.</param>
    /// <param name="gridWalls">Table representing walls/obstacles in grid. See: CartesianGridMovement</param>
    public static void CalculateShortestPathsToTarget(NativeArray<byte> targetDirections, NativeArray<int> targetDistances, int rowCount, CartesianGridCoordinates targetPosition, CartesianGridOnCubeFace cubeFace, byte* gridWalls, float4x4* faceLocalToLocal)
    {
        CalculateShortestWalkableDistancesToTarget(targetDistances, rowCount, gridWalls, targetPosition, cubeFace, faceLocalToLocal );
        CalculateShortestPathGivenDistancesToTarget(targetDirections, rowCount, targetDistances, gridWalls, faceLocalToLocal);
    }

    public static byte LookupDirectionToTarget(int x, int y, int faceIndex, int rowCount, NativeArray<byte> targetDirections)
    {
        var rowStride = ((rowCount + 1) / 2);
        var faceStride = rowCount * rowStride;
        var gridWallsIndex = (faceIndex * faceStride) + (y * rowStride) + (x / 2);

        // Walls in current grid element (odd columns in upper 4 bits of byte)
        var directions = (targetDirections[gridWallsIndex] >> ((x & 1) * 4)) & 0x0f;
        return (byte)directions;
    }

    /// <summary>
    /// Find directions along shortest path(s) to target.
    /// </summary>
    /// <param name="cellPosition">Current position</param>
    /// <param name="rowCount">Height of grid</param>
    /// <param name="targetDirections">Lookup table generated by CalculatePathsToTarget</param>
    /// <returns>Any (or none) best directions along any shortest path in form of CartesianGridDirectionBit</returns>
    public static byte LookupDirectionToTarget(CartesianGridCoordinates cellPosition, CartesianGridOnCubeFace cubeFace, int rowCount, NativeArray<byte> targetDirections)
    {
        return LookupDirectionToTarget(cellPosition.x, cellPosition.y, cubeFace.Value, rowCount, targetDirections);
    }

    public static int LookupDistanceToTarget(int x, int y, int faceIndex, int rowCount, NativeArray<int> targetDistances)
    {
        var rowStride = rowCount;
        var faceStride = rowCount * rowStride;
        var cellIndex = (faceIndex * faceStride) + (y * rowStride) + x;

        return targetDistances[cellIndex];
    }

    /// <summary>
    /// Find directions along shortest path(s) to target.
    /// </summary>
    /// <param name="cellPosition">Current position</param>
    /// <param name="rowCount">Height of grid</param>
    /// <param name="targetDistances">Lookup table generated by CalculatePathsToTarget</param>
    /// <returns>Any (or none) best directions along any shortest path in form of CartesianGridDirectionBit</returns>
    public static int LookupDistanceToTarget(CartesianGridCoordinates cellPosition, CartesianGridOnCubeFace cubeFace, int rowCount, NativeArray<int> targetDistances)
    {
        return LookupDistanceToTarget(cellPosition.x, cellPosition.y, cubeFace.Value, rowCount, targetDistances);
    }

}
