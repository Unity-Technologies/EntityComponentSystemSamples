using System;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateBefore(typeof(CartesianGridMoveForwardSystem))]
public unsafe class CartesianGridOnCubeChangeDirectionSystem : JobComponentSystem
{
    EntityQuery m_GridQuery;
    
    // Next face to move to when moving off edge of a face
    static readonly byte[] m_NextFaceIndex =
    {
        // X+ X- Y+ Y- Z+ Z- <- From which face
        4, 4, 4, 4, 3, 2, // Off north edge
        5, 5, 5, 5, 2, 3, // Off south edge
        2, 3, 1, 0, 1, 1, // Off west edge
        3, 2, 0, 1, 0, 0, // Off east edge
    };

    static readonly byte[] m_NextFaceDirection =
    {
        // X+ X- Y+ Y- Z+ Z- <- From which face
        2, 3, 0, 1, 1, 0, // Off north edge
        2, 3, 1, 0, 1, 0, // Off south edge
        2, 2, 2, 2, 1, 0, // Off west edge
        3, 3, 3, 3, 1, 0, // Off east edge
    };

    // Arbitrarily select direction when two directions are equally valid
    int m_NextPathCounter = 0;

    protected override void OnCreate()
    {
        m_GridQuery = GetEntityQuery(ComponentType.ReadOnly<CartesianGridOnCube>());
        RequireForUpdate(m_GridQuery);
    }
    
    protected override JobHandle OnUpdate(JobHandle lastJobHandle)
    {
        int pathOffset = CartesianGridMovement.NextPathIndex(ref m_NextPathCounter);

        // Get component data from the Grid (GridCube or GridCube)
        var cartesianGridCube = GetSingleton<CartesianGridOnCube>();
        var rowCount = cartesianGridCube.Blob.Value.RowCount;
        var rowStride = ((rowCount + 1) / 2);
        var gridWalls = (byte*)cartesianGridCube.Blob.Value.Walls.GetUnsafePtr();
        var trailingOffsets = (float2*)cartesianGridCube.Blob.Value.TrailingOffsets.GetUnsafePtr();
        var faceLocalToLocal = (float4x4*)cartesianGridCube.Blob.Value.FaceLocalToLocal.GetUnsafePtr();
        
        // Offset center to grid cell
        var cellCenterOffset = new float2(((float)rowCount * 0.5f) - 0.5f, ((float)rowCount * 0.5f) - 0.5f);

        lastJobHandle = Entities
            .WithName("CartesianGridOnCubeChangeDirection")
            .WithNativeDisableUnsafePtrRestriction(gridWalls)
            .WithNativeDisableUnsafePtrRestriction(faceLocalToLocal)
            .WithNativeDisableUnsafePtrRestriction(trailingOffsets)
            .ForEach((ref CartesianGridDirection gridDirection,
                ref Translation translation,
                ref CartesianGridCoordinates gridCoordinates,
                ref CubeFace cubeFace) =>
            {
                var prevDir = gridDirection.Value;
                var nextGridPosition = new CartesianGridCoordinates(translation.Value.xz + trailingOffsets[prevDir], rowCount, rowCount);
                if (gridCoordinates.Equals(nextGridPosition))
                {
                    // Don't allow translation to drift
                    translation.Value = CartesianGridMovement.ClampToGrid(translation.Value, prevDir, gridCoordinates, cellCenterOffset);
                    return; // Still in the same grid cell. No need to change direction.
                }

                // Which edge of GridCube face is being exited (if any)
                var edge = -1;

                // Edge is in order specified in m_NextFaceIndex and m_NextFaceDirection 
                // - Matches GridDirection values.

                edge = math.select(edge, 0, nextGridPosition.y >= rowCount);
                edge = math.select(edge, 1, nextGridPosition.y < 0);
                edge = math.select(edge, 2, nextGridPosition.x < 0);
                edge = math.select(edge, 3, nextGridPosition.x >= rowCount);

                // Change direction based on wall layout (within current face.)
                if (edge == -1)
                {
                    gridCoordinates = nextGridPosition;
                    gridDirection.Value = CartesianGridMovement.LookupGridDirectionFromWalls(gridCoordinates, prevDir, rowStride, gridWalls, pathOffset);
                }

                // Exiting face of GridCube, change face and direction relative to new face.
                else
                {
                    int prevFaceIndex = cubeFace.Value;

                    // Look up next direction given previous face and exit edge.
                    var nextDir = m_NextFaceDirection[(edge * 6) + prevFaceIndex];
                    gridDirection.Value = nextDir;

                    // Lookup next face index given previous face and exit edge.
                    var nextFaceIndex = m_NextFaceIndex[(edge * 6) + prevFaceIndex];
                    cubeFace.Value = nextFaceIndex;

                    // Transform translation relative to next face's grid-space
                    // - This transform is only done to "smooth" the transition around the edges.
                    // - Alternatively, you could "snap" to the same relative position in the next face by rotating the translation components.
                    // - Note that Y position won't be at target value from one edge to another, so that is interpolated in movement update,
                    //   purely for "smoothing" purposes.
                    var localToLocal = faceLocalToLocal[((prevFaceIndex * 6) + nextFaceIndex)];
                    translation.Value.xyz = math.mul(localToLocal, new float4(translation.Value, 1.0f)).xyz;

                    // Update gridPosition relative to new face.
                    gridCoordinates = new CartesianGridCoordinates(translation.Value.xz + trailingOffsets[nextDir], rowCount, rowCount);
                }
            }).Schedule(lastJobHandle);

        return lastJobHandle;
    }
}
