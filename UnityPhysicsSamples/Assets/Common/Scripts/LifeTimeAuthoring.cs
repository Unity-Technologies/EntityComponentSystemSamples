using System;
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
    EntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate() =>
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

    protected override void OnUpdate()
    {
        EntityCommandBuffer.Concurrent commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();

        Entities
            .WithName("DestroyExpiredLifeTime")
            .ForEach((Entity entity, int nativeThreadIndex, ref LifeTime timer) =>
        {
            timer.Value -= 1;

            if (timer.Value < 0f)
            {
                commandBuffer.DestroyEntity(nativeThreadIndex, entity);
            }
        }).ScheduleParallel();

        m_EntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}
