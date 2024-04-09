using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Loading;
using Unity.Mathematics;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Streaming.AssetManagement
{
#if !UNITY_DISABLE_MANAGED_COMPONENTS
    public partial struct AssetLoadingSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var noLoadingQuery = SystemAPI.QueryBuilder().WithAll<References>()
                .WithNone<Loading, RequestUnload>().Build();

            // This uncommented line would add the Loading components, but they would all be null:
            //      state.EntityManager.AddComponent<Loading>(noLoadingQuery);

            // Instead we must add a new Loading to each entity individually:
            foreach (var entity in noLoadingQuery.ToEntityArray(Allocator.Temp))
            {
                state.EntityManager.AddComponentData(entity, new Loading());
            }

            var query = SystemAPI.QueryBuilder().WithAll<References, Loading>()
                .WithNone<RequestUnload>().Build();
            var referencesArray = query.ToComponentDataArray<References>(Allocator.Temp);
            var loadingArray = query.ToComponentArray<Loading>();

            // We cannot use SystemAPI.Query for this loop because we need to
            // call methods that make structural changes in the loop.
            for (int index = 0; index < referencesArray.Length; ++index)
            {
                var refs = referencesArray[index];
                var loading = loadingArray[index];

                // Load Entity Scene
                if (loading.EntityScene == Entity.Null && refs.EntitySceneReference.IsReferenceValid)
                {
                    loading.EntityScene =
                        SceneSystem.LoadSceneAsync(state.WorldUnmanaged, refs.EntitySceneReference);
                }

                // Load Entity Prefab
                if (refs.EntityPrefabReference.IsReferenceValid)
                {
                    if (loading.EntityPrefab == Entity.Null)
                    {
                        loading.EntityPrefab =
                            SceneSystem.LoadPrefabAsync(state.WorldUnmanaged, refs.EntityPrefabReference);
                    }
                    else if (loading.EntityPrefabInstance == Entity.Null &&
                             SceneSystem.GetSceneStreamingState(state.WorldUnmanaged, loading.EntityPrefab) ==
                             SceneSystem.SceneStreamingState.LoadedSuccessfully)
                    {
                        var prefabRoot = state.EntityManager.GetComponentData<PrefabRoot>(loading.EntityPrefab);
                        loading.EntityPrefabInstance = state.EntityManager.Instantiate(prefabRoot.Root);
                    }
                }

                // Load GameObject Scene
                if (!loading.GameObjectScene.IsValid() && refs.GameObjectSceneReference.IsReferenceValid)
                {
                    loading.GameObjectScene = refs.GameObjectSceneReference.LoadAsync(new ContentSceneParameters
                    {
                        autoIntegrate = true, loadSceneMode = LoadSceneMode.Additive,
                        localPhysicsMode = LocalPhysicsMode.None
                    });
                }

                // Load GameObject Prefab
                if (loading.GameObjectPrefabInstance == null &&
                    refs.GameObjectPrefabReference.IsReferenceValid)
                {
                    if (refs.GameObjectPrefabReference.LoadingStatus == ObjectLoadingStatus.None)
                    {
                        refs.GameObjectPrefabReference.LoadAsync();
                    }

                    if (refs.GameObjectPrefabReference.LoadingStatus == ObjectLoadingStatus.Completed)
                    {
                        loading.GameObjectPrefabInstance =
                            Object.Instantiate(refs.GameObjectPrefabReference.Result);
                    }
                }

                // Load Shader
                if (loading.ShaderInstance == null && refs.ShaderReference.IsReferenceValid)
                {
                    if (refs.ShaderReference.LoadingStatus == ObjectLoadingStatus.None)
                    {
                        refs.ShaderReference.LoadAsync();
                    }
                    else if (refs.ShaderReference.LoadingStatus == ObjectLoadingStatus.Completed)
                    {
                        // Create an object to display the loaded Texture
                        loading.ShaderInstance = refs.ShaderReference.Result;
                    }
                }

                // Load Mesh
                float instancesXOffset = 2.5f;
                if (loading.MeshGameObjectInstance == null && refs.MeshReference.IsReferenceValid)
                {
                    if (refs.MeshReference.LoadingStatus == ObjectLoadingStatus.None)
                    {
                        refs.MeshReference.LoadAsync();
                    }
                    else if (refs.MeshReference.LoadingStatus == ObjectLoadingStatus.Completed &&
                             loading.ShaderInstance)
                    {
                        // Create an object to display the loaded Mesh
                        loading.MeshGameObjectInstance = CreateObjectWithMesh(
                            refs.MeshReference.Result,
                            loading.ShaderInstance,
                            "MeshGameObjectInstance",
                            new float3(instancesXOffset, 0f, 0f),
                            quaternion.RotateY(3.1416f / 2)
                        );
                    }
                }

                // Load Material
                if (loading.MaterialGameObjectInstance == null &&
                    refs.MaterialReference.IsReferenceValid)
                {
                    if (refs.MaterialReference.LoadingStatus == ObjectLoadingStatus.None)
                        refs.MaterialReference.LoadAsync();
                    else if (refs.MaterialReference.LoadingStatus == ObjectLoadingStatus.Completed)
                    {
                        // Create an object to display the loaded Material
                        loading.MaterialGameObjectInstance = CreateObjectWithMaterial(
                            refs.MaterialReference.Result,
                            "MaterialGameObjectInstance",
                            new float3(instancesXOffset, 2f, 0f)
                        );
                    }
                }

                // Load Texture
                if (loading.TextureGameObjectInstance == null && refs.TextureReference.IsReferenceValid)
                {
                    if (refs.TextureReference.LoadingStatus == ObjectLoadingStatus.None)
                    {
                        refs.TextureReference.LoadAsync();
                    }
                    else if (refs.TextureReference.LoadingStatus == ObjectLoadingStatus.Completed &&
                             loading.ShaderInstance)
                    {
                        // Create an object to display the loaded Texture
                        loading.TextureGameObjectInstance = CreateObjectWithTexture(
                            refs.TextureReference.Result,
                            loading.ShaderInstance,
                            "TextureGameObjectInstance",
                            new float3(instancesXOffset, 4f, 0f)
                        );
                    }
                }
            }
        }

        public GameObject CreateObjectWithMesh(Mesh mesh, Shader shader, string name, float3 position,
            quaternion rotation)
        {
            // Create an object to display the mesh
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            var transform = obj.transform;
            transform.position = position;
            transform.rotation = rotation;
            var meshFilter = obj.GetComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(shader);
            return obj;
        }

        public GameObject CreateObjectWithMaterial(Material material, string name, float3 position)
        {
            // Create an object to display the material
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            var transform = obj.transform;
            transform.position = position;
            var meshRenderer = obj.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            return obj;
        }

        public GameObject CreateObjectWithTexture(Texture texture, Shader shader, string name, float3 position)
        {
            // Create an object to display the texture
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            var transform = obj.transform;
            transform.position = position;
            var meshRenderer = obj.GetComponent<MeshRenderer>();
            var material = new Material(shader);
            material.mainTexture = texture;
            meshRenderer.sharedMaterial = material;
            return obj;
        }
    }
#endif
}
