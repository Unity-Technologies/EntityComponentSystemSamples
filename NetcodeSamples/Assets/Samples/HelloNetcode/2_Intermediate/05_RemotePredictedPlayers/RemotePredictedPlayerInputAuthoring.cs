using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    [DisallowMultipleComponent]
    public class RemotePredictedPlayerInputAuthoring : MonoBehaviour
    {
        class Baker : Baker<RemotePredictedPlayerInputAuthoring>
        {
            public override void Bake(RemotePredictedPlayerInputAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<RemotePredictedPlayerInput>(entity);
            }
        }
    }
}
