using Unity.Entities;
using UnityEngine;

namespace CrossQuery
{
    public struct PrefabCollection : IComponentData
    {
        public Entity Box;
    }

    public class PrefabCollectionAuthoring : MonoBehaviour
    {
        public GameObject Box;

        class Baker : Baker<PrefabCollectionAuthoring>
        {
            public override void Bake(PrefabCollectionAuthoring authoring)
            {
                PrefabCollection component = default;
                component.Box = GetEntity(authoring.Box);
                AddComponent(component);
            }
        }
    }
}

