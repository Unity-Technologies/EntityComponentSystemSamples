using System.Collections.Generic;
using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;
using Unity.Rendering;
using UnityEngine;

public struct TriggerVolumeChangeMaterial : IComponentData
{
    public Entity ReferenceEntity;
}

public class TriggerVolumeChangeMaterialAuthoring : MonoBehaviour
{
    public GameObject ReferenceGameObject = null;
}

class TriggerVolumeChangeMaterialAuthoringBaker : Baker<TriggerVolumeChangeMaterialAuthoring>
{
    public override void Bake(TriggerVolumeChangeMaterialAuthoring authoring)
    {
        AddComponent(new TriggerVolumeChangeMaterial
        {
            ReferenceEntity = GetEntity(authoring.ReferenceGameObject)
        });
    }
}

[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateAfter(typeof(StatefulTriggerEventBufferSystem))]
public partial class TriggerVolumeChangeMaterialSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem m_CommandBufferSystem;
    private EntityQuery m_NonTriggerQuery;
    private EntityQueryMask m_NonTriggerMask;

    protected override void OnCreate()
    {
        m_CommandBufferSystem = World.GetOrCreateSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();

        m_NonTriggerQuery =
            GetEntityQuery(new EntityQueryDesc
            {
                None = new ComponentType[]
                {
                    typeof(StatefulTriggerEvent)
                }
            });
        Assert.IsFalse(m_NonTriggerQuery.HasFilter(), "The use of EntityQueryMask in this system will not respect the query's active filter settings.");
        m_NonTriggerMask = m_NonTriggerQuery.GetEntityQueryMask();

        RequireForUpdate<TriggerVolumeChangeMaterial>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer commandBuffer = m_CommandBufferSystem.CreateCommandBuffer();

        // Need this extra variable here so that it can
        // be captured by Entities.ForEach loop below
        var nonTriggerMask = m_NonTriggerMask;

        ComponentLookup<MaterialMeshInfo> materialMeshInfoFromEntity = GetComponentLookup<MaterialMeshInfo>();

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
                    if (triggerEvent.State == StatefulEventState.Stay || !nonTriggerMask.MatchesIgnoreFilter(otherEntity))
                    {
                        continue;
                    }

                    if (triggerEvent.State == StatefulEventState.Enter)
                    {
                        MaterialMeshInfo volumeMaterialInfo = materialMeshInfoFromEntity[e];
                        RenderMeshArray volumeRenderMeshArray = EntityManager.GetSharedComponentManaged<RenderMeshArray>(e);

                        MaterialMeshInfo otherMaterialMeshInfo = materialMeshInfoFromEntity[otherEntity];

                        otherMaterialMeshInfo.Material = volumeMaterialInfo.Material;

                        commandBuffer.SetComponent(otherEntity, otherMaterialMeshInfo);
                    }
                    else
                    {
                        // State == PhysicsEventState.Exit
                        if (changeMaterial.ReferenceEntity == Entity.Null)
                        {
                            continue;
                        }

                        MaterialMeshInfo otherMaterialMeshInfo = materialMeshInfoFromEntity[otherEntity];
                        MaterialMeshInfo referenceMaterialMeshInfo = materialMeshInfoFromEntity[changeMaterial.ReferenceEntity];
                        RenderMeshArray referenceRenderMeshArray = EntityManager.GetSharedComponentManaged<RenderMeshArray>(changeMaterial.ReferenceEntity);

                        otherMaterialMeshInfo.Material = referenceMaterialMeshInfo.Material;

                        commandBuffer.SetComponent(otherEntity, otherMaterialMeshInfo);
                    }
                }
            }).Run();

        m_CommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}
