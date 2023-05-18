using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Baking.BakingTypes
{
    public class CompoundBBAuthoring : MonoBehaviour
    {
        class Baker : Baker<CompoundBBAuthoring>
        {
            public override void Bake(CompoundBBAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<CompoundBBComponent>(entity);
            }
        }
    }

    // This component is added to the parent of the entities within a bounding box and
    // stores the bounding box that encompasses them all.
    public struct CompoundBBComponent : IComponentData
    {
        public float3 MinBBVertex;
        public float3 MaxBBVertex;
    }
}
