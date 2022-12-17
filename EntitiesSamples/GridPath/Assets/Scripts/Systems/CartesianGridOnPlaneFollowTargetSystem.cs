using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(CartesianGridChangeDirectionSystemGroup))]
public unsafe partial class CartesianGridOnPlaneFollowTargetSystem : SystemBase
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

    protected override void OnUpdate()
    {
        int pathOffset = m_PathVariationOffset;
        m_PathVariationOffset = (m_PathVariationOffset + 1) & 3;

        // Get component data from the GridPlane
        var cartesianGridPlane = SystemAPI.GetSingleton<CartesianGridOnPlane>();
        var rowCount = cartesianGridPlane.Blob.Value.RowCount;
        var colCount = cartesianGridPlane.Blob.Value.ColCount;
        var trailingOffsets = (float2*)cartesianGridPlane.Blob.Value.TrailingOffsets.GetUnsafePtr();

        var targetEntities = m_TargetQuery.ToEntityArray(World.UpdateAllocator.ToAllocator);
        var targetCoordinates = m_TargetQuery.ToComponentDataArray<CartesianGridCoordinates>(World.UpdateAllocator.ToAllocator);
        var getCartesianGridTargetDirectionFromEntity = GetBufferLookup<CartesianGridTargetDirection>(true);

        // Offset center to grid cell
        var cellCenterOffset = new float2(((float)colCount * 0.5f) - 0.5f, ((float)rowCount * 0.5f) - 0.5f);

        // Whenever a CartesianGridFollowTarget reaches a new grid cell, make a decision about what next direction to turn.
        Entities
            .WithName("ChangeDirectionTowardNearestTarget")
            .WithNativeDisableUnsafePtrRestriction(trailingOffsets)
            .WithEntityQueryOptions(EntityQueryOptions.FilterWriteGroup)
            .WithReadOnly(targetCoordinates)
            .WithReadOnly(targetEntities)
            .WithReadOnly(getCartesianGridTargetDirectionFromEntity)
            .WithAll<CartesianGridFollowTarget>()
            .ForEach((ref CartesianGridDirection gridDirection,
                ref CartesianGridCoordinates gridCoordinates,
#if !ENABLE_TRANSFORM_V1
                ref LocalTransform transform) =>
#else
                ref Translation translation) =>
#endif
                {
                    var dir = gridDirection.Value;
                    if (dir != 0xff) // If moving, update grid based on trailing direction.
                    {
#if !ENABLE_TRANSFORM_V1
                        var nextGridPosition = new CartesianGridCoordinates(transform.Position.xz + trailingOffsets[dir], rowCount, colCount);
                        if (gridCoordinates.Equals(nextGridPosition))
                        {
                            // Don't allow translation to drift
                            transform.Position = CartesianGridMovement.SnapToGridAlongDirection(transform.Position, dir, gridCoordinates, cellCenterOffset);
#else
                        var nextGridPosition = new CartesianGridCoordinates(translation.Value.xz + trailingOffsets[dir], rowCount, colCount);
                        if (gridCoordinates.Equals(nextGridPosition))
                        {
                            // Don't allow translation to drift
                            translation.Value = CartesianGridMovement.SnapToGridAlongDirection(translation.Value, dir, gridCoordinates, cellCenterOffset);
#endif
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
                }).Schedule();
    }
}
