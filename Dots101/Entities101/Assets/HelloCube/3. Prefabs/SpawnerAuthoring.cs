using Unity.Entities;
using UnityEngine;

namespace HelloCube.Prefabs
{
    // An authoring component is just a normal MonoBehavior that has a Baker<T> class.
    public class SpawnerAuthoring : MonoBehaviour
    {
        public GameObject Prefab;

        // In baking, this Baker will run once for every SpawnerAuthoring instance in a subscene.
        // (Note that nesting an authoring component's Baker class inside the authoring MonoBehaviour class
        // is simply an optional matter of style.)
        class Baker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new Spawner
                {
                    Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }

    struct Spawner : IComponentData
    {
        public Entity Prefab;
    }
}
