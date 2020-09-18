using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public struct LifeTime : IComponentData
{
    public int Value;
}

public class LifeTimeAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    [Tooltip("The number of frames until the entity should be destroyed.")]
    public int Value;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) =>
        dstManager.AddComponentData(entity, new LifeTime { Value = Value });
}

public class LifeTimeSystem : SystemBase
{
    protected override void OnUpdate()
    {
        using (var commandBuffer = new EntityCommandBuffer(Allocator.TempJob))
        {
            Entities
                .WithName("DestroyExpiredLifeTime")
                .ForEach((Entity entity, ref LifeTime timer) =>
                {
                    timer.Value -= 1;

                    if (timer.Value < 0f)
                    {
                        commandBuffer.DestroyEntity(entity);
                    }
                }).Run();

            commandBuffer.Playback(EntityManager);
        }
    }
}
