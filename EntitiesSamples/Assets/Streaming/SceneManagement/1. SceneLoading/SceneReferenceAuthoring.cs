using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEditor;
using UnityEngine;

namespace Streaming.SceneManagement.SceneLoading
{
    public class SceneReferenceAuthoring : MonoBehaviour
    {
#if UNITY_EDITOR
        public SceneAsset scene;

        class Baker : Baker<SceneReferenceAuthoring>
        {
            public override void Bake(SceneReferenceAuthoring authoring)
            {
                // We want to create a dependency to the scene in case the scene gets deleted.
                // This needs to be outside the null check below in case the asset file gets deleted and then restored.
                DependsOn(authoring.scene);

                if (authoring.scene != null)
                {
                    var entity = GetEntity(TransformUsageFlags.None);
                    AddComponent(entity, new SceneReference
                    {
                        // Bake a reference to the scene, to be used at runtime to load the scene
                        Value = new EntitySceneReference(authoring.scene)
                    });
                }
            }
        }
#endif
    }

    // Triggers the load of the referenced scene
    public struct SceneReference : IComponentData
    {
        // Reference to the scene to load
        public EntitySceneReference Value;
    }
}
