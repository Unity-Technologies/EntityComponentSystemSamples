using Unity.Entities;
using Unity.Scenes;

namespace Streaming.PrefabAndSceneReferences
{
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial struct LoadingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);

            // request load weak scene references
            foreach (var (weakSceneReference, entity) in
                     SystemAPI.Query<RefRO<SceneReference>>()
                         .WithEntityAccess())
            {
                var sceneEntity = ecb.CreateEntity();
                ecb.AddComponent(sceneEntity, new RequestSceneLoaded());
                ecb.AddComponent(sceneEntity, new Unity.Entities.SceneReference(weakSceneReference.ValueRO.Value));
                ecb.RemoveComponent<SceneReference>(entity);
            }

            // request load weak prefab references
            foreach (var (weakPrefabReference, entity) in
                     SystemAPI.Query<RefRO<PrefabReference>>()
                         .WithNone<RequestEntityPrefabLoaded>()
                         .WithEntityAccess())
            {
                ecb.AddComponent(entity, new RequestEntityPrefabLoaded
                {
                    Prefab = weakPrefabReference.ValueRO.Value
                });
            }

            // instantiated the weak prefab references once they're loaded
            foreach (var (prefabLoadResult, entity) in
                     SystemAPI.Query<RefRO<PrefabLoadResult>>()
                         .WithAll<PrefabReference>()
                         .WithEntityAccess())
            {
                ecb.Instantiate(prefabLoadResult.ValueRO.PrefabRoot);
                ecb.RemoveComponent<PrefabReference>(entity);
            }
        }
    }
}
