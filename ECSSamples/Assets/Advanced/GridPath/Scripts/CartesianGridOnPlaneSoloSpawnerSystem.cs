using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class CartesianGridOnPlaneSoloSpawnerSystem : JobComponentSystem
{
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;
    EntityQuery m_GridQuery;

    protected override void OnCreate()
    {
        // Cache the BeginInitializationEntityCommandBufferSystem in a field, so we don't have to create it every frame
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        m_GridQuery = GetEntityQuery(ComponentType.ReadOnly<CartesianGridOnPlane>());
        RequireForUpdate(m_GridQuery);
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // Clamp delta time so you can't overshoot.
        var deltaTime = math.min(Time.DeltaTime, 0.05f);
        
        var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer();
        var cartesianGridPlane = GetSingleton<CartesianGridOnPlane>();
        var rowCount = cartesianGridPlane.Blob.Value.RowCount;
        var colCount = cartesianGridPlane.Blob.Value.ColCount;
        
        // Offset to center of board
        var cx = (float)colCount * 0.5f;
        var cy = (float)rowCount * 0.5f;        

        Entities.ForEach((ref SoloSpawner soloSpawner) =>
        {
            var secondsUntilGenerate = soloSpawner.SecondsUntilGenerate;
            secondsUntilGenerate -= deltaTime;
            if (secondsUntilGenerate <= 0.0f)
            {
                if (soloSpawner.GeneratedCount < soloSpawner.GenerateMaxCount)
                {
                    var entity = commandBuffer.Instantiate(soloSpawner.Prefab);
                    var u = soloSpawner.Random.NextInt(0, colCount-1);
                    var v = soloSpawner.Random.NextInt(0, rowCount-1);
                    var x = u - cx + 0.5f;
                    var z = v - cy + 0.5f;
                    var y = 1.0f;

                    commandBuffer.SetComponent(entity, new Translation { Value = new float3(x, y, z) });
                    soloSpawner.GeneratedCount++;
                }
                secondsUntilGenerate = soloSpawner.CoolDownSeconds;
            }

            soloSpawner.SecondsUntilGenerate = secondsUntilGenerate;
        }).Run();

        return inputDeps;
    }
}
