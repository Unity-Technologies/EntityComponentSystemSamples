using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct EnableRemotePredictedPlayer : IComponentData { }

    [DisallowMultipleComponent]
    public class EnableRemotePredictedPlayerAuthoring : MonoBehaviour
    {
        class baker : Baker<EnableRemotePredictedPlayerAuthoring>
        {
            public override void Bake(EnableRemotePredictedPlayerAuthoring authoring)
            {
                EnableRemotePredictedPlayer component = default(EnableRemotePredictedPlayer);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
