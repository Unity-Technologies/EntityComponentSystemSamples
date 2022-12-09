using Unity.Entities;
using UnityEngine;

namespace HelloCube.Prefabs
{
    // An authoring component is just a normal MonoBehavior.
    [AddComponentMenu("HelloCube/Spawner")]
    public class SpawnerAuthoring : MonoBehaviour
    {
        public GameObject Prefab;

        // In baking, this Baker will run once for every SpawnerAuthoring instance in an entity subscene.
        // (Nesting an authoring component's Baker class is simply an optional matter of style.)
        class Baker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {
                AddComponent(new Spawner { Prefab = GetEntity(authoring.Prefab) });
            }
        }
    }

    struct Spawner : IComponentData
    {
        public Entity Prefab;
    }
}
