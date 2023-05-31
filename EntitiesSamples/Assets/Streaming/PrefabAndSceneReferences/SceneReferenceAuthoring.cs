using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace Streaming.PrefabAndSceneReferences
{
    public class SceneReferenceAuthoring : MonoBehaviour
    {
        public SceneAsset SceneAsset;

        class Baker : Baker<SceneReferenceAuthoring>
        {
            public override void Bake(SceneReferenceAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SceneReference
                {
                    // The EntitySceneReferences stores the GUID of the scene.
                    Value = new EntitySceneReference(authoring.SceneAsset)
                });
            }
        }
    }

    struct SceneReference : IComponentData
    {
        public EntitySceneReference Value;
    }
}
#endif
