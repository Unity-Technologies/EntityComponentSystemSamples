#if !UNITY_DISABLE_MANAGED_COMPONENTS

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;

namespace Streaming.SceneManagement.CompleteSample
{
    // This system will load/unload the sections based on their distance to Relevant entities
    [UpdateAfter(typeof(RequestPostLoadSystem))]
    partial struct TileLODSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var loadSection0Query = SystemAPI.QueryBuilder().WithAll<LoadSection0, ResolvedSectionEntity>().Build();

            // Handle the loading of sections 0. They are not part of the LODs and need to be always loaded
            var sceneEntities = loadSection0Query.ToEntityArray(Allocator.Temp);
            foreach (var sceneEntity in sceneEntities)
            {
                var buffer = state.EntityManager.GetBuffer<ResolvedSectionEntity>(sceneEntity);
                if (buffer.Length > 0)
                {
                    state.EntityManager.AddComponent<RequestSceneLoaded>(buffer[0].SectionEntity);

                    // We can't remove it with the query in case some buffer is empty
                    state.EntityManager.RemoveComponent<LoadSection0>(sceneEntity);
                }
            }

            // We need the tile center in each section to check the distance. If it is not already in the section, we copy it into it.
            var noTileEntityQuery = SystemAPI.QueryBuilder().WithAll<TileLODRange, SceneEntityReference>()
                .WithNone<TileEntity>().Build();
            var sectionEntities = noTileEntityQuery.ToEntityArray(Allocator.Temp);
            var sceneEntityReferences = noTileEntityQuery.ToComponentDataArray<SceneEntityReference>(Allocator.Temp);
            for (int index = 0; index < sceneEntityReferences.Length; ++index)
            {
                var tileEntity =
                    state.EntityManager.GetComponentData<TileEntity>(sceneEntityReferences[index].SceneEntity);
                state.EntityManager.AddComponentData(sectionEntities[index], tileEntity);
            }

            // Check sections LOD distances
            NativeHashSet<Entity> toLoad = new NativeHashSet<Entity>(1, Allocator.Temp);

            var sectionQuery = SystemAPI.QueryBuilder().WithAll<TileLODRange, TileEntity, SceneSectionData>()
                .Build();
            sectionEntities = sectionQuery.ToEntityArray(Allocator.Temp);
            var tileEntities = sectionQuery.ToComponentDataArray<TileEntity>(Allocator.Temp);
            var lodRanges = sectionQuery.ToComponentDataArray<TileLODRange>(Allocator.Temp);

            // Find all the sections that should be loaded based on the distances to relevant entitie
            for (int index = 0; index < tileEntities.Length; ++index)
            {
                var distanceComponent =
                    state.EntityManager.GetComponentData<DistanceToRelevant>(tileEntities[index].Value);
                float distanceValueSq = distanceComponent.DistanceSq;
                if (distanceValueSq >= lodRanges[index].LowerRadiusSq &&
                    distanceValueSq < lodRanges[index].HigherRadiusSq)
                {
                    toLoad.Add(sectionEntities[index]);
                }
            }

            // Cache the streaming state of the section
            NativeHashMap<Entity, SceneSystem.SectionStreamingState> streamingStateLookup =
                new NativeHashMap<Entity, SceneSystem.SectionStreamingState>(1, Allocator.Temp);
            foreach (Entity sectionEntity in sectionEntities)
            {
                var sectionState = SceneSystem.GetSectionStreamingState(state.WorldUnmanaged, sectionEntity);
                streamingStateLookup.Add(sectionEntity, sectionState);
            }

            // Load or unload the sections based on the previous distance checks
            foreach (Entity sectionEntity in sectionEntities)
            {
                var sectionState = streamingStateLookup[sectionEntity];
                if (toLoad.Contains(sectionEntity))
                {
                    if (sectionState == SceneSystem.SectionStreamingState.Unloaded)
                    {
                        // We need to load the section
                        state.EntityManager.AddComponent<RequestSceneLoaded>(sectionEntity);
                    }
                }
                else if (sectionState != SceneSystem.SectionStreamingState.Unloaded)
                {
                    // Check neighbours to avoid the previous LOD to unload before the new one is loaded
                    var sceneEntityReference =
                        state.EntityManager.GetComponentData<SceneEntityReference>(sectionEntity);
                    var sceneSectionEntities =
                        state.EntityManager.GetBuffer<ResolvedSectionEntity>(sceneEntityReference.SceneEntity,
                            true);

                    int sectionsLoaded = 0;
                    for (int index = 1; index < sceneSectionEntities.Length; ++index)
                    {
                        var neighbourSectionState = streamingStateLookup[sceneSectionEntities[index].SectionEntity];
                        if (neighbourSectionState == SceneSystem.SectionStreamingState.Loaded)
                            ++sectionsLoaded;
                    }

                    // Unload if there is at least one other section loaded
                    if (sectionsLoaded > 1)
                    {
                        state.EntityManager.RemoveComponent<RequestSceneLoaded>(sectionEntity);
                    }
                }
            }
        }
    }
}

#endif
