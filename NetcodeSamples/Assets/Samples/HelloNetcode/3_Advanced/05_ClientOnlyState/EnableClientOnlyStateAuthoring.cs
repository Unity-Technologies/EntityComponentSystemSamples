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
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                //Enabled the the sample systems
                AddComponent<EnableClientOnlyState>(entity);
                //Enabled the client only backup systems
                AddComponent<EnableClientOnlyBackup>(entity);
            }
        }
    }

    public struct EnableClientOnlyState : IComponentData { }
}
