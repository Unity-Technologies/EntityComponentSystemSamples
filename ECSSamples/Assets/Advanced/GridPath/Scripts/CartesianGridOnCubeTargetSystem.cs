using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[UpdateBefore(typeof(CartesianGridOnPlaneFollowTargetSystem))]
public unsafe class CartesianGridOnCubeTargetSystem : JobComponentSystem
{
    EntityQuery m_GridQuery;
    
    protected override void OnCreate()
    {
        m_GridQuery = GetEntityQuery(ComponentType.ReadOnly<CartesianGridOnCube>());
        RequireForUpdate(m_GridQuery);
    } 
    
    protected override JobHandle OnUpdate(JobHandle lastJobHandle)
    {
        // Get component data from the GridPlane
        var cartesianGridCube = GetSingleton<CartesianGridOnCube>();
        var rowCount = cartesianGridCube.Blob.Value.RowCount;
        var targetDirectionsBufferCapacity = 6 * (((rowCount + 1) / 2) * rowCount);
        var targetDistancesBufferCapacity = 6 * (rowCount * rowCount);
        var gridWalls = (byte*)cartesianGridCube.Blob.Value.Walls.GetUnsafePtr();
        var faceLocalToLocal = (float4x4*)cartesianGridCube.Blob.Value.FaceLocalToLocal.GetUnsafePtr();
        
        Entities
            .WithName("InitializeTargets")
            .WithAll<CartesianGridTarget>()
            .WithAll<CartesianGridOnCubeFace>()
            .WithNone<CartesianGridTargetDirection>()
            .WithNone<CartesianGridTargetDistance>()
            .WithStructuralChanges()
            .ForEach((Entity entity) =>
            {
                var directionBuffer = EntityManager.AddBuffer<CartesianGridTargetDirection>(entity);
                directionBuffer.ResizeUninitialized(targetDirectionsBufferCapacity);
                
                var distanceBuffer = EntityManager.AddBuffer<CartesianGridTargetDistance>(entity);
                distanceBuffer.ResizeUninitialized(targetDistancesBufferCapacity);
            }).Run();
        
        // Rebuild all the paths to the target *only* when the target's grid position changes.
        Entities
            .WithName("UpdateTargetPaths")
            .WithNativeDisableUnsafePtrRestriction(faceLocalToLocal)
            .WithAll<CartesianGridTarget>()
            .ForEach((Entity entity, ref CartesianGridTargetCoordinates prevTargetPosition, in CartesianGridOnCubeFace cubeFace, in CartesianGridCoordinates targetPosition, in DynamicBuffer<CartesianGridTargetDirection> targetDirections, in DynamicBuffer<CartesianGridTargetDistance> targetDistances) =>
            {
                if (prevTargetPosition.Equals(targetPosition))
                    return;

                if (targetPosition.OnGrid(rowCount, rowCount))
                {
                    prevTargetPosition = new CartesianGridTargetCoordinates(targetPosition);
                    CartesianGridOnCubeShortestPath.CalculateShortestPathsToTarget( targetDirections.Reinterpret<byte>().AsNativeArray(), targetDistances.Reinterpret<int>().AsNativeArray(), rowCount, targetPosition, cubeFace, gridWalls, faceLocalToLocal);
                }
            }).Run();

        return lastJobHandle;
    }
}
