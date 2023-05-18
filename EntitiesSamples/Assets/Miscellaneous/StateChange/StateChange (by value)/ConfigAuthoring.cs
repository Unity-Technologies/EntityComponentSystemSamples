using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Miscellaneous.StateChangeValue
{
    public class ConfigAuthoring : MonoBehaviour
    {
        public GameObject Prefab;
        public uint Size;
        public float Radius;

        public class ConfigBaker : Baker<ConfigAuthoring>
        {
            public override void Bake(ConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new Config
                {
                    Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                    Size = authoring.Size,
                    Radius = authoring.Radius
                });
                AddComponent<Hit>(entity);
            }
        }
    }

    public struct Config : IComponentData
    {
        public Entity Prefab;
        public uint Size;
        public float Radius;
    }

    public struct Hit : IComponentData
    {
        public float3 Value;
        public bool ChangedThisFrame;
    }
}
