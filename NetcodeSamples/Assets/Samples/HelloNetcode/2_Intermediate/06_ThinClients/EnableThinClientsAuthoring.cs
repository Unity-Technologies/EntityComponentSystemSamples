using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public class EnableThinClientsAuthoring : MonoBehaviour { }
    class Baker : Baker<EnableThinClientsAuthoring>
    {
        public override void Bake(EnableThinClientsAuthoring authoring)
        {
            AddComponent<EnableThinClients>();
        }
    }
    public struct EnableThinClients : IComponentData { }
}
