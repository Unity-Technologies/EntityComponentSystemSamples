using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(CartesianGridChangeDirectionSystemGroup))]
public unsafe partial class CartesianGridOnPlaneBounceOffWallsSystem : SystemBase
{
    EntityQuery m_GridQuery;

    int m_NextPathCounter = 0;

    protected override void OnCreate()
    {
        m_GridQuery = GetEntityQuery(ComponentType.ReadOnly<CartesianGridOnPlane>());
        RequireForUpdate(m_GridQuery);
    }

    protected override void OnUpdate()
    {
        int pathOffset = CartesianGridMovement.NextPathIndex(ref m_NextPathCounter);

        // Get component data from the Grid (GridPlane or GridCube)
        var cartesianGridPlane = SystemAPI.GetSingleton<CartesianGridOnPlane>();
        var rowCount = cartesianGridPlane.Blob.Value.RowCount;
        var colCount = cartesianGridPlane.Blob.Value.ColCount;
        var gridWalls = (byte*)cartesianGridPlane.Blob.Value.Walls.GetUnsafePtr();
        var trailingOffsets = (float2*)cartesianGridPlane.Blob.Value.TrailingOffsets.GetUnsafePtr();

        // Offset center to grid cell
        var cellCenterOffset = new float2(((float)colCount * 0.5f) - 0.5f, ((float)rowCount * 0.5f) - 0.5f);

        Entities
            .WithName("CartesianGridPlaneChangeDirection")
            .WithNativeDisableUnsafePtrRestriction(trailingOffsets)
            .WithNativeDisableUnsafePtrRestriction(gridWalls)
            .WithEntityQueryOptions(EntityQueryOptions.FilterWriteGroup)
            .ForEach((ref CartesianGridDirection gridDirection,
                ref CartesianGridCoordinates gridCoordinates,
#if !ENABLE_TRANSFORM_V1
                ref LocalTransform transform) =>
#else
                ref Translation translation) =>
#endif
                {
                    var dir = gridDirection.Value;
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
                    gridDirection.Value = CartesianGridMovement.BounceDirectionOffWalls(gridCoordinates, dir, rowCount, colCount, gridWalls, pathOffset);
                }).Schedule();
    }
}
