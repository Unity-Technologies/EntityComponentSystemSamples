using System;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Entities.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Streaming.AssetManagement
{
    public class AssetAuthoring : MonoBehaviour
    {
        [SerializeField] public References References;

        class Baker : Baker<AssetAuthoring>
        {
            public override void Bake(AssetAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.References);
            }
        }
    }

    [Serializable]
    public struct References : IComponentData
    {
        public EntitySceneReference EntitySceneReference;
        public EntityPrefabReference EntityPrefabReference;
        public WeakObjectSceneReference GameObjectSceneReference;
        public WeakObjectReference<GameObject> GameObjectPrefabReference;
        public WeakObjectReference<Mesh> MeshReference;
        public WeakObjectReference<Material> MaterialReference;
        public WeakObjectReference<Texture> TextureReference;
        public WeakObjectReference<Shader> ShaderReference;
    }

    public struct RequestUnload : IComponentData
    {
    }

#if !UNITY_DISABLE_MANAGED_COMPONENTS
    public class Loading : IComponentData
    {
        public Scene GameObjectScene;
        public GameObject GameObjectPrefabInstance;
        public GameObject MeshGameObjectInstance;
        public GameObject MaterialGameObjectInstance;
        public GameObject TextureGameObjectInstance;
        public Entity EntityScene;
        public Entity EntityPrefab;
        public Entity EntityPrefabInstance;
        public Shader ShaderInstance;
    }
#endif
}
