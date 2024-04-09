using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;

namespace Streaming.PrefabAndSceneReferences
{
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial struct LoadingSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<SceneReference>().Build();
            var sceneRefs = query.ToComponentDataArray<SceneReference>(Allocator.Temp);
            var entities = query.ToEntityArray(Allocator.Temp);
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Load entity scene and add a cleanup component referencing the entity scene to
            // unload when the primary entity will be destroyed
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var sceneEntity = SceneSystem.LoadSceneAsync(state.World.Unmanaged, sceneRefs[i].Value);
                ecb.AddComponent(entity, new CleanupSceneReference()
                {
                    SceneToUnload = sceneEntity
                });
                ecb.RemoveComponent<SceneReference>(entity);
            }

            // Load the PrefabReferences
            foreach (var (prefabRef, entity) in
                     SystemAPI.Query<RefRO<PrefabReference>>()
                         .WithNone<RequestEntityPrefabLoaded>()
                         .WithEntityAccess())
            {
                ecb.AddComponent(entity, new RequestEntityPrefabLoaded
                {
                    Prefab = prefabRef.ValueRO.Value
                });
            }

            // Instantiate the PrefabReferences
            foreach (var (loadedPrefab, entity) in
                     SystemAPI.Query<RefRO<PrefabLoadResult>>()
                         .WithAll<PrefabReference>()
                         .WithEntityAccess())
            {
                var prefabEntity = ecb.Instantiate(loadedPrefab.ValueRO.PrefabRoot);
                ecb.AddComponent(entity, new CleanupPrefabReference()
                {
                    PrefabToUnload = prefabEntity
                });
                ecb.RemoveComponent<PrefabReference>(entity);
                ecb.RemoveComponent<RequestEntityPrefabLoaded>(entity);
            }

            ecb.Playback(state.EntityManager);

            // Unload the previously manually loaded entity scene after the subscene is being destroyed
            query = SystemAPI.QueryBuilder().WithAll<CleanupSceneReference>().WithNone<SceneTag>().Build();
            var cleanupSceneRefs = query.ToComponentDataArray<CleanupSceneReference>(Allocator.Temp);
            entities = query.ToEntityArray(Allocator.Temp);
            ecb = new EntityCommandBuffer(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var cleanupSceneRef = cleanupSceneRefs[i];
                SceneSystem.UnloadScene(state.World.Unmanaged, cleanupSceneRef.SceneToUnload);
                ecb.DestroyEntity(cleanupSceneRef.SceneToUnload);
                ecb.RemoveComponent<CleanupSceneReference>(entities[i]);
            }

            // Unload the previously manually instantiated entity prefabs after the subscene is being destroyed
            foreach (var (prefabRef, entity) in
                     SystemAPI.Query<RefRO<CleanupPrefabReference>>()
                         .WithNone<SceneTag>()
                         .WithEntityAccess())
            {
                ecb.DestroyEntity(prefabRef.ValueRO.PrefabToUnload);
                ecb.RemoveComponent<CleanupPrefabReference>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
