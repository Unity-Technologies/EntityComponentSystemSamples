using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(CartesianGridChangeDirectionSystemGroup))]
public unsafe partial class CartesianGridOnCubeBounceOffWallsSystem : SystemBase
{
    EntityQuery m_GridQuery;

    // Arbitrarily select direction when two directions are equally valid
    int m_NextPathCounter = 0;

    protected override void OnCreate()
    {
        m_GridQuery = GetEntityQuery(ComponentType.ReadOnly<CartesianGridOnCube>());
        RequireForUpdate(m_GridQuery);
    }

    protected override void OnUpdate()
    {
        int pathOffset = CartesianGridMovement.NextPathIndex(ref m_NextPathCounter);

        // Get component data from the Grid (GridCube or GridCube)
        var cartesianGridCube = GetSingleton<CartesianGridOnCube>();
        var rowCount = cartesianGridCube.Blob.Value.RowCount;
        var gridWalls = (byte*)cartesianGridCube.Blob.Value.Walls.GetUnsafePtr();
        var trailingOffsets = (float2*)cartesianGridCube.Blob.Value.TrailingOffsets.GetUnsafePtr();
        var faceLocalToLocal = (float4x4*)cartesianGridCube.Blob.Value.FaceLocalToLocal.GetUnsafePtr();

        // Offset center to grid cell
        var cellCenterOffset = new float2(((float)rowCount * 0.5f) - 0.5f, ((float)rowCount * 0.5f) - 0.5f);

        Entities
            .WithName("CartesianGridOnCubeChangeDirection")
            .WithNativeDisableUnsafePtrRestriction(gridWalls)
            .WithNativeDisableUnsafePtrRestriction(faceLocalToLocal)
            .WithNativeDisableUnsafePtrRestriction(trailingOffsets)
            .WithEntityQueryOptions(EntityQueryOptions.FilterWriteGroup)
            .ForEach((ref CartesianGridDirection gridDirection,
#if !ENABLE_TRANSFORM_V1
                ref LocalToWorldTransform transform,
#else
                ref Translation translation,
#endif
                ref CartesianGridCoordinates gridCoordinates,
                ref CartesianGridOnCubeFace cubeFace) =>
                {
                    var prevDir = gridDirection.Value;
                    var trailingOffset = trailingOffsets[prevDir];
#if !ENABLE_TRANSFORM_V1
                    var pos = transform.Value.Position.xz + trailingOffset;
#else
                    var pos = translation.Value.xz + trailingOffset;
#endif
                    var nextGridPosition = new CartesianGridCoordinates(pos, rowCount, rowCount);
                    if (gridCoordinates.Equals(nextGridPosition))
                    {
                        // Don't allow translation to drift
#if !ENABLE_TRANSFORM_V1
                        transform.Value.Position = CartesianGridMovement.SnapToGridAlongDirection(transform.Value.Position, prevDir, gridCoordinates, cellCenterOffset);
#else
                        translation.Value = CartesianGridMovement.SnapToGridAlongDirection(translation.Value, prevDir, gridCoordinates, cellCenterOffset);
#endif
                        return; // Still in the same grid cell. No need to change direction.
                    }

                    var edge = CartesianGridMovement.CubeExitEdge(nextGridPosition, rowCount);

                    // Change direction based on wall layout (within current face.)
                    if (edge == -1)
                    {
                        var faceIndex = cubeFace.Value;
                        var rowStride = (rowCount + 1) / 2;
                        var faceStride = rowCount * rowStride;
                        var faceGridWallsOffset = faceIndex * faceStride;

                        gridCoordinates = nextGridPosition;
                        gridDirection.Value = CartesianGridMovement.BounceDirectionOffWalls(gridCoordinates, prevDir, rowCount, rowCount, gridWalls + faceGridWallsOffset, pathOffset);
                    }
                    // Exiting face of GridCube, change face and direction relative to new face.
                    else
                    {
                        int prevFaceIndex = cubeFace.Value;

                        // Look up next direction given previous face and exit edge.
                        var nextDir = CartesianGridOnCubeUtility.NextFaceDirection[(edge * 6) + prevFaceIndex];
                        gridDirection.Value = nextDir;

                        // Lookup next face index given previous face and exit edge.
                        var nextFaceIndex = CartesianGridOnCubeUtility.NextFaceIndex[(edge * 6) + prevFaceIndex];
                        cubeFace.Value = nextFaceIndex;

                        // Transform translation relative to next face's grid-space
                        // - This transform is only done to "smooth" the transition around the edges.
                        // - Alternatively, you could "snap" to the same relative position in the next face by rotating the translation components.
                        // - Note that Y position won't be at target value from one edge to another, so that is interpolated in movement update,
                        //   purely for "smoothing" purposes.
                        var localToLocal = faceLocalToLocal[((prevFaceIndex * 6) + nextFaceIndex)];
#if !ENABLE_TRANSFORM_V1
                        transform.Value.Position.xyz = math.mul(localToLocal, new float4(transform.Value.Position, 1.0f)).xyz;

                        // Update gridPosition relative to new face.
                        gridCoordinates = new CartesianGridCoordinates(transform.Value.Position.xz + trailingOffsets[nextDir], rowCount, rowCount);
#else
                        translation.Value.xyz = math.mul(localToLocal, new float4(translation.Value, 1.0f)).xyz;

                        // Update gridPosition relative to new face.
                        gridCoordinates = new CartesianGridCoordinates(translation.Value.xz + trailingOffsets[nextDir], rowCount, rowCount);
#endif
                    }
                }).Schedule();
    }
}
