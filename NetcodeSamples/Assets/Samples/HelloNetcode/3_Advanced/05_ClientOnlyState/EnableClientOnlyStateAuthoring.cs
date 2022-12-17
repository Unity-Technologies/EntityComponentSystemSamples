using Unity.Entities;
using Unity.NetCode.Samples;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public class EnableClientOnlyStateAuthoring: MonoBehaviour
    {
        class Baker : Baker<EnableClientOnlyStateAuthoring>
        {
            public override void Bake(EnableClientOnlyStateAuthoring authoring)
            {
                //Enabled the the sample systems
                AddComponent<EnableClientOnlyState>();
                //Enabled the client only backup systems
                AddComponent<EnableClientOnlyBackup>();
            }
        }
    }

    public struct EnableClientOnlyState : IComponentData { }
}
