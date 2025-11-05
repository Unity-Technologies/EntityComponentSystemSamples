using UnityEngine;
using Unity.Entities;

namespace Unity.Physics.Tests
{
    /// <summary>
    /// This component is used to store a prefab for testing its conversion into an entity.
    /// It allows verification that the resulting entity, marked with the [Prefab] tag, was correctly converted.
    /// For example, it is used in the [Built-in Prefab Joint Conversion.scene] to validate the conversion process.
    /// </summary>
    public class PrefabAuthoring : MonoBehaviour
    {
        public GameObject Prefab;

        public class PrefabAuthoringBaker : Baker<PrefabAuthoring>
        {
            public override void Bake(PrefabAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PrefabComponentData
                {
                    Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }

    // This component is used to store a prefab entity reference.
    public struct PrefabComponentData : IComponentData
    {
        public Entity Prefab;
    }
}
