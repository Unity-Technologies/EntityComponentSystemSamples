using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public class EnableThinClientsAuthoring : MonoBehaviour { }
    class Baker : Baker<EnableThinClientsAuthoring>
    {
        public override void Bake(EnableThinClientsAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<EnableThinClients>(entity);
        }
    }
    public struct EnableThinClients : IComponentData { }
}
