using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Streaming.RuntimeContentManager
{
    // This will create a number of entities within the bounds of the BoxCollider using random
    // meshes and materials from the properties. The number of entities is determined by the spacing value.
    [RequireComponent(typeof(BoxCollider))]
    public class SpawnAreaAuthoring : MonoBehaviour
    {
        public float spacing = 10;
        public List<WeakObjectReference<Mesh>> meshes;
        public List<WeakObjectReference<Material>> materials;

        class Baker : Baker<SpawnAreaAuthoring>
        {
            public override void Bake(SpawnAreaAuthoring authoring)
            {
                var collider = authoring.GetComponent<BoxCollider>();

                if (collider == null || authoring.meshes.Count == 0 || authoring.materials.Count == 0)
                {
                    return;
                }

                DependsOnComponentInChildren<Transform>();
                DependsOnComponentInChildren<BoxCollider>();

                var bounds = collider.bounds;
                bounds.center = authoring.transform.position;

                for (float z = bounds.min.z; z < bounds.max.z; z += authoring.spacing)
                {
                    for (float y = bounds.min.y; y < bounds.max.y; y += authoring.spacing)
                    {
                        for (float x = bounds.min.x; x < bounds.max.x; x += authoring.spacing)
                        {
                            if (math.length(new float2(x, y)) < 10)
                            {
                                continue;
                            }

                            var entity = CreateAdditionalEntity(TransformUsageFlags.ManualOverride);
                            var index = UnityEngine.Random.Range(0, authoring.meshes.Count);

                            AddComponent(entity, new DecorationVisualComponentData
                            {
                                mesh = authoring.meshes[index],
                                material = authoring.materials[index],
                                loaded = false,
                                withinLoadRange = false
                            });
                            AddComponent(entity, new LocalToWorld
                            {
                                Value = new float4x4(quaternion.identity, new float3(x, y, z))
                            });
                        }
                    }
                }
            }
        }
    }

    // contains the loading and visibility state as well as the mesh and material
    public struct DecorationVisualComponentData : IComponentData
    {
        public bool withinLoadRange;
        public bool loaded;
        public bool shouldRender;
        public WeakObjectReference<Mesh> mesh;
        public WeakObjectReference<Material> material;
    }
}
