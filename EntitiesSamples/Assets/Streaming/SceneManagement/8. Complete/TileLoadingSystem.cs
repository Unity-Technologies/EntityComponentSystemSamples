using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;

namespace Streaming.SceneManagement.CompleteSample
{
    // This system will load/unload the tile scenes based on the tile distance to Relevant entities
    [UpdateAfter(typeof(TileDistanceSystem))]
    partial struct TileLoadingSystem : ISystem
    {
        ComponentTypeSet loadComponentTypeSet;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            loadComponentTypeSet = new ComponentTypeSet(ComponentType.ReadWrite<RequiresPostLoadCommandBuffer>(),
                ComponentType.ReadWrite<TileEntity>(),
                ComponentType.ReadWrite<LoadSection0>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var sectionQuery = SystemAPI.QueryBuilder().WithAll<TileInfo, DistanceToRelevant>().Build();

            var tileEntities = sectionQuery.ToEntityArray(Allocator.Temp);
            var tileInfos = sectionQuery.ToComponentDataArray<TileInfo>(Allocator.Temp);
            var tileDistances = sectionQuery.ToComponentDataArray<DistanceToRelevant>(Allocator.Temp).Reinterpret<float>();

            NativeList<LoadableTile> priorityLoadList = new NativeList<LoadableTile>(tileEntities.Length, Allocator.Temp);

            // Find all the scenes that should be loaded/unloaded based on the distances to relevant entities
            for (int index = 0; index < tileInfos.Length; ++index)
            {
                if (tileDistances[index] < tileInfos[index].LoadingDistanceSq)
                {
                    priorityLoadList.Add(new LoadableTile
                    {
                        TileIndex = index,
                        DistanceSq = tileDistances[index]
                    });
                }
                else if (tileDistances[index] > tileInfos[index].UnloadingDistanceSq)
                {
                    // Check if the tile has been loaded before
                    if (state.EntityManager.HasComponent<SubsceneEntity>(tileEntities[index]))
                    {
                        // We unload the scene
                        var complexSceneSubsceneEntity =
                            state.EntityManager.GetComponentData<SubsceneEntity>(tileEntities[index]);
                        SceneSystem.UnloadScene(state.WorldUnmanaged, complexSceneSubsceneEntity.Value, SceneSystem.UnloadParameters.DestroyMetaEntities);
                        state.EntityManager.RemoveComponent<SubsceneEntity>(tileEntities[index]);
                    }
                }
            }

            // Prioritize loading sections closest to the relevant entities
            priorityLoadList.Sort(new SectionDistanceComparer());

            // Load
            int loading = 0;
            int maxToLoad = 4; // limit how many we load at one time
            foreach (var loadEntry in priorityLoadList)
            {
                int tileIndex = loadEntry.TileIndex;
                if (!state.EntityManager.HasComponent<SubsceneEntity>(tileEntities[tileIndex]))
                {
                    // Load the Scene as a new instance and disable the auto loading of the sections
                    var sceneEntity = SceneSystem.LoadSceneAsync(state.WorldUnmanaged,
                        tileInfos[tileIndex].Scene, new SceneSystem.LoadParameters()
                        {
                            Flags = SceneLoadFlags.NewInstance | SceneLoadFlags.DisableAutoLoad
                        });

                    float3 center = new float3(tileInfos[tileIndex].Position.x, 0f, tileInfos[tileIndex].Position.y);

                    state.EntityManager.AddComponent(sceneEntity, loadComponentTypeSet);

                    // Postpone adding the PostLoadCommandBuffer so most of the load code can be Bursted
                    state.EntityManager.SetComponentData(sceneEntity, new RequiresPostLoadCommandBuffer
                    {
                        Position = center,
                        Rotation = tileInfos[tileIndex].Rotation
                    });

                    // We also store the center of the tile, to make it accessible for distance checks on it during the section loading
                    state.EntityManager.SetComponentData(sceneEntity, new TileEntity
                    {
                        Value = tileEntities[tileIndex]
                    });

                    // We store the scene entity to handle the unload later
                    state.EntityManager.AddComponentData(tileEntities[tileIndex], new SubsceneEntity
                    {
                        Value = sceneEntity
                    });

                    // Increase the loading count, so we only load a limit number of scenes simultaneously
                    ++loading;
                }
                else
                {
                    // We need to check if the current tile is loading
                    var subSceneEntityComponent = state.EntityManager.GetComponentData<SubsceneEntity>(tileEntities[tileIndex]);
                    var streamingState = SceneSystem.GetSceneStreamingState(state.WorldUnmanaged, subSceneEntityComponent.Value);
                    if (streamingState != SceneSystem.SceneStreamingState.LoadedSuccessfully && streamingState != SceneSystem.SceneStreamingState.LoadedSectionEntities)
                    {
                        // Increase the loading count, so we only load a limit number of scenes simultaneously
                        ++loading;
                    }
                }

                if (loading >= maxToLoad)
                {
                    break;
                }
            }
        }
    }
}
