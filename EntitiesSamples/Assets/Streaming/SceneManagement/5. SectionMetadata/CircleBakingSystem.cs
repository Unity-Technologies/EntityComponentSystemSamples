using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;

namespace Streaming.SceneManagement.SectionMetadata
{
    // Adds each circle to the metadata entity of its section.
    // (It is assumed each circle belongs to a different section.)
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    partial struct CircleBakingSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Cleanup from previous baking.
            var cleanupQuery = SystemAPI.QueryBuilder().WithAll<Circle, SectionMetadataSetup>().Build();
            state.EntityManager.RemoveComponent<Circle>(cleanupQuery);

            var circleQuery = SystemAPI.QueryBuilder().WithAll<Circle, SceneSection>().Build();
            var circles = circleQuery.ToComponentDataArray<Circle>(Allocator.Temp);
            var circleEntities = circleQuery.ToEntityArray(Allocator.Temp);

            var sectionQuery = SystemAPI.QueryBuilder().WithAll<SectionMetadataSetup>().Build();

            for (int index = 0; index < circleEntities.Length; ++index)
            {
                var sceneSection = state.EntityManager.GetSharedComponent<SceneSection>(circleEntities[index]);
                var sectionEntity = SerializeUtility.GetSceneSectionEntity(sceneSection.Section, state.EntityManager,
                    ref sectionQuery, true);
                state.EntityManager.AddComponentData(sectionEntity, circles[index]);
            }
        }
    }
}
