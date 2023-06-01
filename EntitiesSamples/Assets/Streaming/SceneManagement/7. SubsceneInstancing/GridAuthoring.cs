using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using UnityEngine;

namespace Streaming.SceneManagement.SubsceneInstancing
{
    public class GridAuthoring : MonoBehaviour
    {
#if UNITY_EDITOR
        public UnityEditor.SceneAsset scene; // the subscene to instantiate
        public int size;  // number of instances will be size x size
        public float2 spacing;  // distance between the instances

        class Baker : Baker<GridAuthoring>
        {
            public override void Bake(GridAuthoring authoring)
            {
                // We need a dependency on the scene in case the scene gets deleted.
                // This needs to be outside the authoring.scene != null check in case the asset file gets deleted and then restored.
                DependsOn(authoring.scene);

                if (authoring.scene != null)
                {
                    var entity = GetEntity(TransformUsageFlags.Dynamic);
                    AddComponent(entity, new Grid
                    {
                        Scene = new EntitySceneReference(authoring.scene),
                        Size = authoring.size,
                        Spacing = authoring.spacing
                    });
                }
            }
        }
#endif
    }

    public struct Grid : IComponentData
    {
        public EntitySceneReference Scene;
        public int Size;
        public float2 Spacing;
    }

    public struct Offset : IComponentData
    {
        public float3 Value;
    }
}
