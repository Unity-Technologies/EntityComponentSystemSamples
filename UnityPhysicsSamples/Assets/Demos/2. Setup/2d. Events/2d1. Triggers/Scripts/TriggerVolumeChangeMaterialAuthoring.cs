using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Stateful;
using Unity.Rendering;
using UnityEngine;

public struct TriggerVolumeChangeMaterial : IComponentData
{
    public Entity ReferenceEntity;
}

public class TriggerVolumeChangeMaterialAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public GameObject ReferenceGameObject = null;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new TriggerVolumeChangeMaterial
        {
            ReferenceEntity = ReferenceGameObject != null ? conversionSystem.GetPrimaryEntity(ReferenceGameObject) : Entity.Null
        });
    }
}

[UpdateAfter(typeof(TriggerEventConversionSystem))]
public class TriggerVolumeChangeMaterialSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;
    private TriggerEventConversionSystem m_TriggerSystem;
    private EntityQueryMask m_NonTriggerMask;
    private EntityQuery m_TriggerVolumeQuery;

    protected override void OnCreate()
    {
        m_TriggerSystem = World.GetOrCreateSystem<TriggerEventConversionSystem>();
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        m_NonTriggerMask = EntityManager.GetEntityQueryMask(
                GetEntityQuery(new EntityQueryDesc
                {
                    None = new ComponentType[]
                    {
                        typeof(StatefulTriggerEvent)
                    }
                })
            );
        RequireForUpdate(GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(TriggerVolumeChangeMaterial)
            }
        }));
    }

    protected override void OnUpdate()
    {
        Dependency = JobHandle.CombineDependencies(m_TriggerSystem.OutDependency, Dependency);

        var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer();

        // Need this extra variable here so that it can
        // be captured by Entities.ForEach loop below
        var nonTriggerMask = m_NonTriggerMask;

        Entities
            .WithName("ChangeMaterialOnTriggerEnter")
            .WithoutBurst()
            .ForEach((Entity e, ref DynamicBuffer<StatefulTriggerEvent> triggerEventBuffer, ref TriggerVolumeChangeMaterial changeMaterial) =>
            {
                for (int i = 0; i < triggerEventBuffer.Length; i++)
                {
                    var triggerEvent = triggerEventBuffer[i];
                    var otherEntity = triggerEvent.GetOtherEntity(e);

                    // exclude other triggers and processed events
                    if (triggerEvent.State == EventOverlapState.Stay || !nonTriggerMask.Matches(otherEntity))
                    {
                        continue;
                    }

                    if (triggerEvent.State == EventOverlapState.Enter)
                    {
                        var volumeRenderMesh = EntityManager.GetSharedComponentData<RenderMesh>(e);
                        var overlappingRenderMesh = EntityManager.GetSharedComponentData<RenderMesh>(otherEntity);
                        overlappingRenderMesh.material = volumeRenderMesh.material;

                        commandBuffer.SetSharedComponent(otherEntity, overlappingRenderMesh);
                    }
                    else
                    {
                        // State == PhysicsEventState.Exit
                        if (changeMaterial.ReferenceEntity == Entity.Null)
                        {
                            continue;
                        }
                        var overlappingRenderMesh = EntityManager.GetSharedComponentData<RenderMesh>(otherEntity);
                        var referenceRenderMesh = EntityManager.GetSharedComponentData<RenderMesh>(changeMaterial.ReferenceEntity);
                        overlappingRenderMesh.material = referenceRenderMesh.material;

                        commandBuffer.SetSharedComponent(otherEntity, overlappingRenderMesh);
                    }
                }
            }).Run();
    }
}
