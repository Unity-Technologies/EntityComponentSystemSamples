using Unity.Entities;
using UnityEngine;

namespace Graphical.PrefabInitializer
{
    public class SpawnerAuthoring : MonoBehaviour
    {
        public GameObject Prefab;

        class Baker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Spawner
                {
                    Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.None)
                });
            }
        }
    }

    public struct Spawner : IComponentData
    {
        public Entity Prefab;
    }
}
