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
                AddComponent<RemotePredictedPlayerInput>();
            }
        }
    }
}
