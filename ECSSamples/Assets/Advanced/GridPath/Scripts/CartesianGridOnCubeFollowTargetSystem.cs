using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(CartesianGridChangeDirectionSystemGroup))]
public unsafe class CartesianGridOnPCubeFollowTargetSystem : JobComponentSystem
{
    EntityQuery m_GridQuery;
    EntityQuery m_TargetQuery;
    int m_PathVariationOffset = 0;

    protected override void OnCreate()
    {
        m_GridQuery = GetEntityQuery(ComponentType.ReadOnly<CartesianGridOnCube>());
        m_TargetQuery = GetEntityQuery(ComponentType.ReadOnly<CartesianGridTargetDirection>(), ComponentType.ReadOnly<CartesianGridCoordinates>());
        RequireForUpdate(m_GridQuery);
    }

    static Entity FindTargetShortestPathLength(CartesianGridCoordinates gridCoordinates, CartesianGridOnCubeFace cubeFace, int rowCount, NativeArray<CartesianGridCoordinates> targetCoordinates, NativeArray<Entity> targetEntities, BufferFromEntity<CartesianGridTargetDistance> getCartesianGridTargetDistanceFromEntity) 
    {
        var targetEntity = Entity.Null;
        if (!gridCoordinates.OnGrid(rowCount, rowCount))
            return targetEntity;
            
        var targetBestDistance = 6*((rowCount * rowCount) + 1);
        for (int i = 0; i < targetCoordinates.Length; i++)
        {
            // Note targets will be invisible (off grid) when transitioning between cube faces
            if (!targetCoordinates[i].OnGrid(rowCount, rowCount))
                continue;
            
            var targetDistances = getCartesianGridTargetDistanceFromEntity[targetEntities[i]].Reinterpret<int>().AsNativeArray();
            var targetDistance = CartesianGridOnCubeShortestPath.LookupDistanceToTarget(gridCoordinates, cubeFace, rowCount, targetDistances);
            if (targetDistance < targetBestDistance)
            {
                targetEntity = targetEntities[i];
                targetBestDistance = targetDistance;
            }
        }
        return targetEntity;
    }
    
    protected override JobHandle OnUpdate(JobHandle lastJobHandle)
    {
        int pathOffset = m_PathVariationOffset;
        m_PathVariationOffset = (m_PathVariationOffset + 1) & 3;

        // Get component data from the GridCube
        var cartesianGridCube = GetSingleton<CartesianGridOnCube>();
        var rowCount = cartesianGridCube.Blob.Value.RowCount;
        var gridWalls = (byte*)cartesianGridCube.Blob.Value.Walls.GetUnsafePtr();
        var trailingOffsets = (float2*)cartesianGridCube.Blob.Value.TrailingOffsets.GetUnsafePtr();
        var faceLocalToLocal = (float4x4*)cartesianGridCube.Blob.Value.FaceLocalToLocal.GetUnsafePtr();

        var targetEntities = m_TargetQuery.ToEntityArray(Allocator.TempJob);
        var targetCoordinates = m_TargetQuery.ToComponentDataArray<CartesianGridCoordinates>(Allocator.TempJob);
        var getCartesianGridTargetDirectionFromEntity = GetBufferFromEntity<CartesianGridTargetDirection>(true);
        var getCartesianGridTargetDistanceFromEntity = GetBufferFromEntity<CartesianGridTargetDistance>(true);
        
        // Offset center to grid cell
        var cellCenterOffset = new float2(((float)rowCount * 0.5f) - 0.5f, ((float)rowCount * 0.5f) - 0.5f);

        // Whenever a CartesianGridFollowTarget reaches a new grid cell, make a decision about what next direction to turn.
        lastJobHandle = Entities
            .WithName("ChangeDirectionTowardNearestTarget")
            .WithNativeDisableUnsafePtrRestriction(trailingOffsets)
            .WithNativeDisableUnsafePtrRestriction(faceLocalToLocal)
            .WithNativeDisableUnsafePtrRestriction(gridWalls)
            .WithEntityQueryOptions(EntityQueryOptions.FilterWriteGroup)
            .WithReadOnly(targetCoordinates)
            .WithReadOnly(getCartesianGridTargetDirectionFromEntity)
            .WithReadOnly(getCartesianGridTargetDistanceFromEntity)
            .WithAll<CartesianGridFollowTarget>()
            .ForEach((ref CartesianGridDirection gridDirection,
                ref CartesianGridCoordinates gridCoordinates,
                ref Translation translation,
                ref CartesianGridOnCubeFace cubeFace) =>
            {
                var dir = gridDirection.Value;
                if (dir != 0xff) // If moving, update grid based on trailing direction.
                {
                    var nextGridPosition = new CartesianGridCoordinates(translation.Value.xz + trailingOffsets[dir], rowCount, rowCount);
                    if (gridCoordinates.Equals(nextGridPosition))
                    {
                        // Don't allow translation to drift
                        translation.Value = CartesianGridMovement.SnapToGridAlongDirection(translation.Value, dir, gridCoordinates, cellCenterOffset);
                        return; // Still in the same grid cell. No need to change direction.
                    }

                    var edge = CartesianGridMovement.CubeExitEdge(nextGridPosition, rowCount);

                    // Change direction based on wall layout (within current face.)
                    if (edge == -1)
                    {
                        gridCoordinates = nextGridPosition;
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
                        translation.Value.xyz = math.mul(localToLocal, new float4(translation.Value, 1.0f)).xyz;

                        // Update gridPosition relative to new face.
                        gridCoordinates = new CartesianGridCoordinates(translation.Value.xz + trailingOffsets[nextDir], rowCount, rowCount);
                    }
                }

                if (!gridCoordinates.OnGrid(rowCount, rowCount))
                    return;

                var targetEntity = FindTargetShortestPathLength(gridCoordinates, cubeFace, rowCount, targetCoordinates, targetEntities, getCartesianGridTargetDistanceFromEntity);
                if (targetEntity == Entity.Null)
                {
                    // No target for whatever reason, look busy.
                    int faceIndex = cubeFace.Value;
                    var rowStride = (rowCount + 1) / 2;
                    var faceStride = rowCount * rowStride;
                    var faceGridWallsOffset = faceIndex * faceStride;
                    gridDirection.Value = CartesianGridMovement.BounceDirectionOffWalls(gridCoordinates, dir, rowCount, rowCount, gridWalls + faceGridWallsOffset, pathOffset);
                    return;
                }

                // Lookup next direction along shortest path to target from table stored in CartesianGridTargetDirection 
                // - When multiple shortest path available, use pathOffset to select which option.
                var targetDirections = getCartesianGridTargetDirectionFromEntity[targetEntity].Reinterpret<byte>().AsNativeArray();
                var validDirections = CartesianGridOnCubeShortestPath.LookupDirectionToTarget(gridCoordinates, cubeFace, rowCount, targetDirections);
                gridDirection.Value = CartesianGridMovement.PathVariation[(pathOffset * 16) + validDirections];
            }).Schedule(lastJobHandle);

        lastJobHandle = targetEntities.Dispose(lastJobHandle);
        lastJobHandle = targetCoordinates.Dispose(lastJobHandle);

        return lastJobHandle;
    }
}
