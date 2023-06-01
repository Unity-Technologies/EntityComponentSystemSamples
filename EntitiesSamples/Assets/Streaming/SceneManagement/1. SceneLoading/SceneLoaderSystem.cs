using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;

namespace Streaming.SceneManagement.SceneLoading
{
    public partial struct SceneLoaderSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SceneReference>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Loads all the requested scenes and removes the requests from the entities
            var query = SystemAPI.QueryBuilder().WithAll<SceneReference>().Build();
            var requests = query.ToComponentDataArray<SceneReference>(Allocator.Temp);
            for (int i = 0; i < requests.Length; i += 1)
            {
                // Creates an entity with scene-related components, which will later trigger scene loading
                // in the scene loading systems.
                // (Because this method may add a component to an entity, it cannot be called in a foreach query.)
                SceneSystem.LoadSceneAsync(state.WorldUnmanaged, requests[i].Value);
            }
            state.EntityManager.DestroyEntity(query);
        }
    }
}
