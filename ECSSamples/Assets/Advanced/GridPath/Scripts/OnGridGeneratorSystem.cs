using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class OnGridGeneratorSystem : JobComponentSystem
{
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        RequireSingletonForUpdate<GridConfig>();
        
        // Cache the BeginInitializationEntityCommandBufferSystem in a field, so we don't have to create it every frame
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var deltaTime = Time.DeltaTime;
        var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer();
        var gridEntity = GetSingletonEntity<GridConfig>();
        var gridConfig = EntityManager.GetComponentData<GridConfig>(gridEntity);
        var onGridCube = EntityManager.HasComponent<GridCube>(gridEntity);
        var onGridPlane = EntityManager.HasComponent<GridPlane>(gridEntity);
        
        // Board size
        var rowCount = gridConfig.RowCount;
        var colCount = gridConfig.ColCount;
        
        // Offset to center of board
        var cx = (float)colCount * 0.5f;
        var cy = (float)rowCount * 0.5f;        

        Entities.ForEach((ref OnGridGenerator onGridGenerator) =>
        {
            var secondsUntilGenerate = onGridGenerator.SecondsUntilGenerate;
            secondsUntilGenerate -= deltaTime;
            if (secondsUntilGenerate <= 0.0f)
            {
                if (onGridGenerator.GeneratedCount < onGridGenerator.GenerateMaxCount)
                {
                    var entity = commandBuffer.Instantiate(onGridGenerator.Prefab);
                    var u = onGridGenerator.Random.NextInt(0, colCount-1);
                    var v = onGridGenerator.Random.NextInt(0, rowCount-1);
                    var x = u - cx + 0.5f;
                    var z = v - cy + 0.5f;
                    var y = 1.0f;

                    if (onGridCube)
                    {
                        var faceIndex = onGridGenerator.Random.NextInt(0, 6);
                        commandBuffer.AddComponent(entity, new GridCube());
                        commandBuffer.AddComponent(entity, new GridFace { Value = (byte)faceIndex });
                    }
                    
                    if (onGridPlane)
                        commandBuffer.AddComponent(entity, new GridPlane());
                    
                    commandBuffer.SetComponent(entity, new Translation { Value = new float3(x, y, z) });
                    onGridGenerator.GeneratedCount++;
                }
                secondsUntilGenerate = onGridGenerator.CoolDownSeconds;
            }

            onGridGenerator.SecondsUntilGenerate = secondsUntilGenerate;
        }).Run();

        return inputDeps;
    }
}
