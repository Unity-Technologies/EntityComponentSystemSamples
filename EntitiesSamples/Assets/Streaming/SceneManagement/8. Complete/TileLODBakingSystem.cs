using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;

namespace Streaming.SceneManagement.CompleteSample
{
    // This system will store the LOD distances meta data for the sections
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    partial struct TileLODBakingSystem : ISystem
    {
        // Cannot be Burst-compiled because it calls SerializeUtility.GetSceneSectionEntity
        public void OnUpdate(ref SystemState state)
        {
            // Remove all the previously stored data, for incremental baking
            var cleaningQuery =  SystemAPI.QueryBuilder().WithAll<TileLODRange, SectionMetadataSetup>().Build();
            state.EntityManager.RemoveComponent<TileLODBaking>(cleaningQuery);

            var radiusQuery = SystemAPI.QueryBuilder().WithAll<TileLODBaking>().Build();
            var sectionLODs = radiusQuery.ToComponentDataArray<TileLODBaking>(Allocator.Temp);

            EntityQuery sectionEntityQuery = default;
            for (int index = 0; index < sectionLODs.Length; ++index)
            {
                // Get the section entity during baking
                var sectionEntity = SerializeUtility.GetSceneSectionEntity(sectionLODs[index].Section,
                    state.EntityManager, ref sectionEntityQuery, true);

                // Add the meta information to the sections
                var lowerRadius = sectionLODs[index].LowerRadius;
                var higherRadius = sectionLODs[index].HigherRadius;
                state.EntityManager.AddComponentData(sectionEntity, new TileLODRange
                {
                    LowerRadiusSq = lowerRadius * lowerRadius,
                    HigherRadiusSq = higherRadius * higherRadius
                });
            }
        }
    }
}
