using Unity.Entities;
using UnityEngine;

namespace HelloCube.CrossQuery
{
    public class PrefabCollectionAuthoring : MonoBehaviour
    {
        public GameObject Box;

        class Baker : Baker<PrefabCollectionAuthoring>
        {
            public override void Bake(PrefabCollectionAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                PrefabCollection component = default;
                component.Box = GetEntity(authoring.Box, TransformUsageFlags.Dynamic);

                AddComponent(entity, component);
            }
        }
    }

    public struct PrefabCollection : IComponentData
    {
        public Entity Box;
    }
}

