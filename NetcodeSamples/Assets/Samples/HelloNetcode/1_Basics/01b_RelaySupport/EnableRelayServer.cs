using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct EnableRelayServer : IComponentData { }

    // Each sample system is enabled by adding these types of enable components to the entity
    // scene. This prevents all the systems in all the samples to run simultaneously all the time.
    // Each sample can then also enable systems from previous samples by adding it's enable component.
    public class EnableRelayServerAuthoring : MonoBehaviour
    {
        class Baker : Baker<EnableRelayServerAuthoring>
        {
            public override void Bake(EnableRelayServerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<EnableRelayServer>(entity);
            }
        }
    }
}
