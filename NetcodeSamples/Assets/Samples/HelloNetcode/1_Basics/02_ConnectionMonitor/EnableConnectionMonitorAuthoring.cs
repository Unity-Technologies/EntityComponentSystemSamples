using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct EnableConnectionMonitor : IComponentData { }

    [DisallowMultipleComponent]
    public class EnableConnectionMonitorAuthoring : MonoBehaviour
    {
        class Baker : Baker<EnableConnectionMonitorAuthoring>
        {
            public override void Bake(EnableConnectionMonitorAuthoring authoring)
            {
                EnableConnectionMonitor component = default(EnableConnectionMonitor);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
