using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;

namespace Streaming.SceneManagement.SceneState
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct SceneStateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SceneReference>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var sceneQuery = SystemAPI.QueryBuilder().WithAll<SceneReference>().Build();
            var scenes = sceneQuery.ToComponentDataArray<SceneReference>(Allocator.Temp);

            // We cannot use a foreach query here because the SceneSystem methods add and remove components,
            // which is not allowed inside a foreach query.
            for (int index = 0; index < scenes.Length; ++index)
            {
                var scene = scenes[index];
                scene.StreamingState = SceneSystem.GetSceneStreamingState(state.WorldUnmanaged, scene.EntityScene);

                // The LoadingAction is set when the user clicks a button in the UI.
                switch (scene.LoadingAction)
                {
                    case LoadingAction.LoadAll:
                    case LoadingAction.LoadMeta:
                    {
                        var loadParam = new SceneSystem.LoadParameters
                        {
                            AutoLoad = (scene.LoadingAction == LoadingAction.LoadAll)
                        };
                        if (scene.EntityScene == default)
                        {
                            scene.EntityScene =
                                SceneSystem.LoadSceneAsync(state.WorldUnmanaged, scene.SceneAsset, loadParam);
                        }
                        else
                        {
                            SceneSystem.LoadSceneAsync(state.WorldUnmanaged, scene.EntityScene, loadParam);
                        }

                        break;
                    }
                    case LoadingAction.UnloadAll:
                    {
                        SceneSystem.UnloadScene(state.WorldUnmanaged, scene.EntityScene,
                            SceneSystem.UnloadParameters.DestroyMetaEntities);
                        scene.EntityScene = default;
                        break;
                    }
                    case LoadingAction.UnloadEntities:
                    {
                        SceneSystem.UnloadScene(state.WorldUnmanaged, scene.EntityScene);
                        break;
                    }
                }

                scene.LoadingAction = LoadingAction.None;
                scenes[index] = scene;
            }

            // Copy the values in the array back to the actual components.
            sceneQuery.CopyFromComponentDataArray(scenes);
        }
    }
}
