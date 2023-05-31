using Unity.Assertions;
using Unity.Entities;
using Unity.Physics.Stateful;
using Unity.Physics.Systems;
using Unity.Rendering;

namespace Events
{
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(StatefulTriggerEventSystem))]
    public partial struct TriggerVolumeChangeMaterialSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TriggerVolumeChangeMaterial>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var nonTriggerQuery = SystemAPI.QueryBuilder().WithNone<StatefulTriggerEvent>().Build();
            Assert.IsFalse(nonTriggerQuery.HasFilter(),
                "The use of EntityQueryMask in this system will not respect the query's active filter settings.");
            var nonTriggerMask = nonTriggerQuery.GetEntityQueryMask();

            var materialMeshInfoLookup = SystemAPI.GetComponentLookup<MaterialMeshInfo>();

            foreach (var(triggerEventBuffer, changeMaterial, entity) in
                     SystemAPI.Query<DynamicBuffer<StatefulTriggerEvent>, RefRW<TriggerVolumeChangeMaterial>>()
                         .WithEntityAccess())
            {
                for (int i = 0; i < triggerEventBuffer.Length; i++)
                {
                    var triggerEvent = triggerEventBuffer[i];
                    var otherEntity = triggerEvent.GetOtherEntity(entity);

                    // exclude other triggers and processed events
                    if (triggerEvent.State == StatefulEventState.Stay ||
                        !nonTriggerMask.MatchesIgnoreFilter(otherEntity))
                    {
                        continue;
                    }

                    if (triggerEvent.State == StatefulEventState.Enter)
                    {
                        MaterialMeshInfo volumeMaterialInfo = materialMeshInfoLookup[entity];
                        RenderMeshArray volumeRenderMeshArray =
                            state.EntityManager.GetSharedComponentManaged<RenderMeshArray>(entity);

                        MaterialMeshInfo otherMaterialMeshInfo = materialMeshInfoLookup[otherEntity];

                        otherMaterialMeshInfo.Material = volumeMaterialInfo.Material;

                        ecb.SetComponent(otherEntity, otherMaterialMeshInfo);
                    }
                    else
                    {
                        // State == PhysicsEventState.Exit
                        if (changeMaterial.ValueRW.ReferenceEntity == Entity.Null)
                        {
                            continue;
                        }

                        MaterialMeshInfo otherMaterialMeshInfo = materialMeshInfoLookup[otherEntity];
                        MaterialMeshInfo referenceMaterialMeshInfo =
                            materialMeshInfoLookup[changeMaterial.ValueRW.ReferenceEntity];
                        RenderMeshArray referenceRenderMeshArray =
                            state.EntityManager.GetSharedComponentManaged<RenderMeshArray>(
                                changeMaterial.ValueRW.ReferenceEntity);

                        otherMaterialMeshInfo.Material = referenceMaterialMeshInfo.Material;

                        ecb.SetComponent(otherEntity, otherMaterialMeshInfo);
                    }
                }
            }
        }
    }
}
