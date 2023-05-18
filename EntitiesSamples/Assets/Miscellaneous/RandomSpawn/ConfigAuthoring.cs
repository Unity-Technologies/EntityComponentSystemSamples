using Unity.Entities;
using UnityEngine;

namespace Miscellaneous.RandomSpawn
{
    public class ConfigAuthoring : MonoBehaviour
    {
        public GameObject Prefab;

        class Baker : Baker<ConfigAuthoring>
        {
            public override void Bake(ConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new Config
                {
                    Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }

    public struct Config : IComponentData
    {
        public Entity Prefab;
    }
}
