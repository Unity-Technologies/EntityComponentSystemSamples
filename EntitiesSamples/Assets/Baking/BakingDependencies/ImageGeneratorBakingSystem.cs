using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

namespace Baking.BakingDependencies
{
#if !UNITY_DISABLE_MANAGED_COMPONENTS
    // This is a baking system, a system that runs only in the baking world, after the bakers have run. It provides
    // more flexibility (e.g. accessing any entity) but doesn't allow expressing dependencies. This is the reason why
    // this sample relies on communication between the baker (registering dependencies) and the system (doing the
    // setup required by the renderable entities, for which no API exists to do so directly in the baker).
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct ImageGeneratorBakingSystem : ISystem
    {
        EntityQuery m_ImageGeneratorEntitiesQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Gathering all the entities that have been processed by the ImageGenerator baker.
            m_ImageGeneratorEntitiesQuery =
                SystemAPI.QueryBuilder().WithAll<ImageGeneratorEntity, MeshArrayBakingType>().Build();

            // Filter for entities for which the ImageGeneratorEntity buffer has been updated. In other words,
            // the entities for which the baker has run during this baking pass. Alternatively, we could filter for the
            // MeshArrayBakingType component instead, since it's added by the same baker.
            m_ImageGeneratorEntitiesQuery.SetChangedVersionFilter(ComponentType.ReadOnly<ImageGeneratorEntity>());
            state.RequireForUpdate(m_ImageGeneratorEntitiesQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            var renderMeshDescription = new RenderMeshDescription(UnityEngine.Rendering.ShadowCastingMode.Off);
            var materialMeshInfo = MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0);

            // Since the following loops does structural changes, the contents of the query cannot be iterated with SystemAPI.Query().
            // So instead, each one is processed by doing a random access of the component values. This is more expensive, but
            // the amount of entities is expected to be low. Because there is only one such entity per ImageGenerator, and also
            // because the system being reactive, only the ones that have been updated need to be processed here.
            var entities = m_ImageGeneratorEntitiesQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var bakingType = SystemAPI.ManagedAPI.GetComponent<MeshArrayBakingType>(entity);
                var bakingEntities = SystemAPI.GetBuffer<ImageGeneratorEntity>(entity).Reinterpret<Entity>()
                    .ToNativeArray(Allocator.Temp);

                foreach (var bakingEntity in bakingEntities)
                {
                    RenderMeshUtility.AddComponents(bakingEntity, state.EntityManager, renderMeshDescription,
                        bakingType.meshArray, materialMeshInfo);
                }
            }
        }
    }
#endif
}
