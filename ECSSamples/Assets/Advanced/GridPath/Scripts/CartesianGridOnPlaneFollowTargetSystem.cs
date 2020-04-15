using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(CartesianGridChangeDirectionSystemGroup))]
public unsafe class CartesianGridOnPlaneFollowTargetSystem : JobComponentSystem
{
    EntityQuery m_GridQuery;
    EntityQuery m_TargetQuery;
    int m_PathVariationOffset = 0;

    protected override void OnCreate()
    {
        m_GridQuery = GetEntityQuery(ComponentType.ReadOnly<CartesianGridOnPlane>());
        m_TargetQuery = GetEntityQuery(ComponentType.ReadOnly<CartesianGridTargetDirection>(), ComponentType.ReadOnly<CartesianGridCoordinates>());
        RequireForUpdate(m_GridQuery);
    }

    static Entity FindTargetShortestManhattanDistance(CartesianGridCoordinates gridCoordinates, int rowCount, int colCount, NativeArray<CartesianGridCoordinates> targetCoordinates, NativeArray<Entity> targetEntities)
    {
        var targetEntity = Entity.Null;
        var targetBestDistance = (rowCount * colCount) + 1;
        for (int i = 0; i < targetCoordinates.Length; i++)
        {
            var targetDistance = math.abs(targetCoordinates[i].x - gridCoordinates.x) + math.abs(targetCoordinates[i].y - gridCoordinates.y);
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

        // Get component data from the GridPlane
        var cartesianGridPlane = GetSingleton<CartesianGridOnPlane>();
        var rowCount = cartesianGridPlane.Blob.Value.RowCount;
        var colCount = cartesianGridPlane.Blob.Value.ColCount;
        var trailingOffsets = (float2*)cartesianGridPlane.Blob.Value.TrailingOffsets.GetUnsafePtr();

        var targetEntities = m_TargetQuery.ToEntityArray(Allocator.TempJob);
        var targetCoordinates = m_TargetQuery.ToComponentDataArray<CartesianGridCoordinates>(Allocator.TempJob);
        var getCartesianGridTargetDirectionFromEntity = GetBufferFromEntity<CartesianGridTargetDirection>(true);

        // Offset center to grid cell
        var cellCenterOffset = new float2(((float)colCount * 0.5f) - 0.5f, ((float)rowCount * 0.5f) - 0.5f);

        // Whenever a CartesianGridFollowTarget reaches a new grid cell, make a decision about what next direction to turn.
        lastJobHandle = Entities
            .WithName("ChangeDirectionTowardNearestTarget")
            .WithNativeDisableUnsafePtrRestriction(trailingOffsets)
            .WithEntityQueryOptions(EntityQueryOptions.FilterWriteGroup)
            .WithReadOnly(targetCoordinates)
            .WithReadOnly(getCartesianGridTargetDirectionFromEntity)
            .WithAll<CartesianGridFollowTarget>()
            .ForEach((ref CartesianGridDirection gridDirection,
                ref CartesianGridCoordinates gridCoordinates,
                ref Translation translation) =>
            {
                var dir = gridDirection.Value;
                if (dir != 0xff) // If moving, update grid based on trailing direction.
                {
                    var nextGridPosition = new CartesianGridCoordinates(translation.Value.xz + trailingOffsets[dir], rowCount, colCount);
                    if (gridCoordinates.Equals(nextGridPosition))
                    {
                        // Don't allow translation to drift
                        translation.Value = CartesianGridMovement.SnapToGridAlongDirection(translation.Value, dir, gridCoordinates, cellCenterOffset);
                        return; // Still in the same grid cell. No need to change direction.
                    }

                    gridCoordinates = nextGridPosition;
                }

                var targetEntity = FindTargetShortestManhattanDistance(gridCoordinates, rowCount, colCount, targetCoordinates, targetEntities);
                if (targetEntity == Entity.Null)
                {
                    // No target for whatever reason, don't move.
                    gridDirection.Value = 0xff;
                    return;
                }

                // Lookup next direction along shortest path to target from table stored in CartesianGridTargetDirection 
                // - When multiple shortest path available, use pathOffset to select which option.
                var targetDirections = getCartesianGridTargetDirectionFromEntity[targetEntity].Reinterpret<byte>().AsNativeArray();
                var validDirections = CartesianGridOnPlaneShortestPath.LookupDirectionToTarget(gridCoordinates, rowCount, colCount, targetDirections);
                gridDirection.Value = CartesianGridMovement.PathVariation[(pathOffset * 16) + validDirections];
            }).Schedule(lastJobHandle);

        lastJobHandle = targetEntities.Dispose(lastJobHandle);
        lastJobHandle = targetCoordinates.Dispose(lastJobHandle);

        return lastJobHandle;
    }
}
