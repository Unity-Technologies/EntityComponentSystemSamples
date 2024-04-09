using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Scenes;
using UnityEngine;

namespace Streaming.AssetManagement
{
#if !UNITY_DISABLE_MANAGED_COMPONENTS
    public partial struct AssetUnloadingSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<References, Loading, RequestUnload>().Build();
            var referencesArray = query.ToComponentDataArray<References>(Allocator.Temp);
            var loadingArray = query.ToComponentArray<Loading>();

            // We cannot use SystemAPI.Query for this loop because we need to
            // call methods that make structural changes in the loop.
            for (int index = 0; index < referencesArray.Length; ++index)
            {
                var refs = referencesArray[index];
                var loading = loadingArray[index];

                // Unload Entity Scene
                if (loading.EntityScene != Entity.Null)
                {
                    SceneSystem.UnloadScene(state.WorldUnmanaged, loading.EntityScene,
                        SceneSystem.UnloadParameters.DestroyMetaEntities);
                }

                // Unload Entity Prefab
                if (loading.EntityPrefabInstance != Entity.Null)
                {
                    state.EntityManager.DestroyEntity(loading.EntityPrefabInstance);
                }

                if (loading.EntityPrefab != Entity.Null)
                {
                    SceneSystem.UnloadScene(state.WorldUnmanaged, loading.EntityPrefab,
                        SceneSystem.UnloadParameters.DestroyMetaEntities);
                }

                // Unload GameObject Scene
                if (loading.GameObjectScene.IsValid())
                {
                    refs.GameObjectSceneReference.Unload(ref loading.GameObjectScene);
                }

                // Unload GameObject Prefab
                if (loading.GameObjectPrefabInstance != null)
                {
                    Object.Destroy(loading.GameObjectPrefabInstance);
                }

                if (refs.GameObjectPrefabReference.LoadingStatus != ObjectLoadingStatus.None)
                {
                    refs.GameObjectPrefabReference.Release();
                }

                // Unload Mesh
                if (loading.MeshGameObjectInstance)
                {
                    var renderer = loading.MeshGameObjectInstance.GetComponent<MeshRenderer>();
                    var material = renderer.sharedMaterial;
                    Object.Destroy(material);
                    GameObject.Destroy(loading.MeshGameObjectInstance);
                }

                if (refs.MeshReference.LoadingStatus != ObjectLoadingStatus.None)
                {
                    refs.MeshReference.Release();
                }

                // Unload Material
                if (loading.MaterialGameObjectInstance)
                {
                    GameObject.Destroy(loading.MaterialGameObjectInstance);
                }

                if (refs.MaterialReference.LoadingStatus != ObjectLoadingStatus.None)
                {
                    refs.MaterialReference.Release();
                }

                // Unload Texture
                if (loading.TextureGameObjectInstance)
                {
                    var renderer = loading.TextureGameObjectInstance.GetComponent<MeshRenderer>();
                    var material = renderer.sharedMaterial;
                    Object.Destroy(material);
                    GameObject.Destroy(loading.TextureGameObjectInstance);
                }

                if (refs.TextureReference.LoadingStatus != ObjectLoadingStatus.None)
                {
                    refs.TextureReference.Release();
                }

                // Unload Shader
                if (refs.ShaderReference.LoadingStatus != ObjectLoadingStatus.None)
                {
                    refs.ShaderReference.Release();
                }
            }

            // Remove Loading
            var noLoadingStateQuery = SystemAPI.QueryBuilder()
                .WithAll<References, Loading, RequestUnload>().Build();
            state.EntityManager.RemoveComponent<Loading>(noLoadingStateQuery);
        }
    }
#endif
}
