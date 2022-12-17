using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;


namespace HelloCube.BakingTypes
{
    public class CompoundBBAuthoring : MonoBehaviour
    {
        class Baker : Baker<CompoundBBAuthoring>
        {
            public override void Bake(CompoundBBAuthoring authoring)
            {
                AddComponent<CompoundBBComponent>();
            }
        }
    }
    
    /// This component is added to the parent of the objects within a bounding box. It stores the combined bounding box.
    public struct CompoundBBComponent : IComponentData
    {
        public float3 MinBBVertex;
        public float3 MaxBBVertex;
    }
}
