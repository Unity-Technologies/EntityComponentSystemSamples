using Unity.Entities;
using UnityEngine;

namespace StateMachineValue
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
                AddComponent(new Config
                {
                    Prefab = GetEntity(authoring.Prefab),
                    Size = authoring.Size,
                    Radius = authoring.Radius
                });
            }
        }
    }

    public struct Config : IComponentData
    {
        public Entity Prefab;
        public uint Size;
        public float Radius;
    }
}