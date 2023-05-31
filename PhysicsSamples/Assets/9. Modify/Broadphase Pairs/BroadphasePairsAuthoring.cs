using Unity.Entities;
using UnityEngine;

namespace Modify
{
    public class BroadphasePairsAuthoring : MonoBehaviour
    {
        class Baker : Baker<BroadphasePairsAuthoring>
        {
            public override void Bake(BroadphasePairsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new BroadphasePairs());
            }
        }
    }

    public struct BroadphasePairs : IComponentData
    {
    }
}
