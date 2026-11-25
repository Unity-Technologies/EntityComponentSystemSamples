using Unity.Entities;
using UnityEngine;

namespace GameObjectSample
{
    public struct Spawner : IComponentData
    {
        public Entity GhostPrefab1;
        public Entity GhostPrefab2;
    }

    [DisallowMultipleComponent]
    public class SpawnerAuthoring : MonoBehaviour
    {
        public GameObject GhostPrefab1;
        public GameObject GhostPrefab2;

        class Baker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {
                Spawner component = default(Spawner);
                component.GhostPrefab1 = GetEntity(authoring.GhostPrefab1, TransformUsageFlags.Dynamic);
                component.GhostPrefab2 = GetEntity(authoring.GhostPrefab2, TransformUsageFlags.Dynamic);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
