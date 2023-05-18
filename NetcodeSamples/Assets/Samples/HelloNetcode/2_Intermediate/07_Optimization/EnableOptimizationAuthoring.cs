using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct EnableOptimization : IComponentData { }

    [DisallowMultipleComponent]
    public class EnableOptimizationAuthoring : MonoBehaviour
    {
        class Baker : Baker<EnableOptimizationAuthoring>
        {
            public override void Bake(EnableOptimizationAuthoring authoring)
            {
                EnableOptimization component = default(EnableOptimization);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
