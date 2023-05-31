using Unity.Collections;
using Unity.Entities;

namespace Streaming.SceneManagement.SceneState
{
    public partial struct UISystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SceneReference>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (StateUI.Singleton == null)
            {
                return;
            }
            var ui = StateUI.Singleton;

            var sceneQuery = SystemAPI.QueryBuilder().WithAll<SceneReference>().Build();
            var scenes = sceneQuery.ToComponentDataArray<SceneReference>(Allocator.Temp);
            var entities = sceneQuery.ToEntityArray(Allocator.Temp);

            ui.UpdateSceneRows(scenes);
            if (ui.GetAction(out var sceneIndex, out var action))
            {
                var scene = scenes[sceneIndex];
                scene.LoadingAction = action;
                state.EntityManager.SetComponentData(entities[sceneIndex], scene);
            }
        }
    }
}
