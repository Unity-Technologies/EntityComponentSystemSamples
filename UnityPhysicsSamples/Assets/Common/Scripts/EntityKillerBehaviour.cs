using Unity.Physics;
using Unity.Collections;
using Unity.Entities;
using System;
using UnityEngine;
using Unity.Jobs;

public struct EntityKiller : IComponentData
{
    public int TimeToDie;
}

public class EntityKillerBehaviour : MonoBehaviour, IConvertGameObjectToEntity
{
    public int TimeToDie;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData<EntityKiller>(entity, new EntityKiller() { TimeToDie = TimeToDie });
    }
}

public class EntityKillerSystem : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    struct EntityKillerJob : IJobForEachWithEntity<EntityKiller>
    {
        public EntityCommandBuffer CommandBuffer;

        public void Execute(Entity entity, int index, ref EntityKiller killer)
        {
            if (killer.TimeToDie > 0)
            {
                CommandBuffer.SetComponent<EntityKiller>(entity, new EntityKiller() { TimeToDie = killer.TimeToDie - 1 });
            }
            else
            {
                CommandBuffer.DestroyEntity(entity);
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new EntityKillerJob
        {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer()
        }.ScheduleSingle(this, inputDeps);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);

        return job;
    }
}
