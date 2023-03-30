using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct EnableRPC : IComponentData { }

    [DisallowMultipleComponent]
    public class EnableRPCAuthoring : MonoBehaviour
    {
        class Baker : Baker<EnableRPCAuthoring>
        {
            public override void Bake(EnableRPCAuthoring authoring)
            {
                EnableRPC component = default(EnableRPC);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
