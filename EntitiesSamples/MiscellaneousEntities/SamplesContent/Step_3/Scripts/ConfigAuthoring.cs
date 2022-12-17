using Unity.Entities;
using UnityEngine;

namespace RandomSpawn
{
    public class ConfigAuthoring : MonoBehaviour
    {
        public GameObject Prefab;
    }

    public class ConfigBaker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            AddComponent(new Config
            {
                Prefab = GetEntity(authoring.Prefab)
            });
        }
    }

    public struct Config : IComponentData
    {
        public Entity Prefab;
    }
}
