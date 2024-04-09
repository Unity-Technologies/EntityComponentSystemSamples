using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;

namespace Baking.PrefabReference
{
#if UNITY_EDITOR
    public class ConfigAuthoring : MonoBehaviour
    {
        public GameObject Prefab;
        public float SpawnInterval;

        class Baker : Baker<ConfigAuthoring>
        {
            public override void Bake(ConfigAuthoring authoring)
            {
                // Create an EntityPrefabReference from a GameObject.
                // By using a reference, we only need one baked prefab entity instead of
                // duplicating the prefab entity everywhere it is used.
                var prefabEntity = new EntityPrefabReference(authoring.Prefab);

                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new Config
                {
                    PrefabReference = prefabEntity,
                    SpawnInterval = authoring.SpawnInterval
                });
            }
        }
    }
#endif

    public struct Config : IComponentData
    {
        public float SpawnInterval;
        public EntityPrefabReference PrefabReference;
    }
}
