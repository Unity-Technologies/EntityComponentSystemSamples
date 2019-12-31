using Unity.Entities;
using Unity.Jobs;

[UpdateBefore(typeof(CartesianGridOnPlaneFollowTargetSystem))]
public unsafe class CartesianGridOnPlaneTargetSystem : JobComponentSystem
{
    EntityQuery m_GridQuery;
    
    protected override void OnCreate()
    {
        m_GridQuery = GetEntityQuery(ComponentType.ReadOnly<CartesianGridOnPlane>());
        RequireForUpdate(m_GridQuery);
    } 
    
    protected override JobHandle OnUpdate(JobHandle lastJobHandle)
    {
        // Get component data from the GridPlane
        var cartesianGridPlane = GetSingleton<CartesianGridOnPlane>();
        var rowCount = cartesianGridPlane.Blob.Value.RowCount;
        var colCount = cartesianGridPlane.Blob.Value.ColCount;
        var targetDirectionsBufferCapacity = ((colCount + 1) / 2) * rowCount;
        var gridWalls = (byte*)cartesianGridPlane.Blob.Value.Walls.GetUnsafePtr();

        // Initialize the buffer for the target paths with size appropriate to grid.
        Entities
            .WithName("InitializeTargets")
            .WithAll<CartesianGridTarget>()
            .WithNone<CartesianGridTargetDirection>()
            .WithStructuralChanges()
            .ForEach((Entity entity) =>
            {
                var buffer = EntityManager.AddBuffer<CartesianGridTargetDirection>(entity);
                buffer.ResizeUninitialized(targetDirectionsBufferCapacity);
            }).Run();
        
        // Rebuild all the paths to the target *only* when the target's grid position changes.
        Entities
            .WithName("UpdateTargetPaths")
            .WithAll<CartesianGridTarget>()
            .ForEach((Entity entity, ref CartesianGridTargetCoordinates prevTargetPosition, in CartesianGridCoordinates targetPosition, in DynamicBuffer<CartesianGridTargetDirection> targetDirections) =>
            {
                if (prevTargetPosition.Equals(targetPosition))
                    return;
                
                prevTargetPosition = new CartesianGridTargetCoordinates(targetPosition);
                CartesianGridOnPlaneShortestPath.CalculateShortestPathsToTarget(targetDirections.Reinterpret<byte>().AsNativeArray(), rowCount, colCount, targetPosition, gridWalls);
            }).Run();

        return lastJobHandle;
    }
}
