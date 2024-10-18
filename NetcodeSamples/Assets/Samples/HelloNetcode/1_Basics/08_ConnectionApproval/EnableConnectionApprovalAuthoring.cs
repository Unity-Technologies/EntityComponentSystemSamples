using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct EnableConnectionApproval : IComponentData { }

    [DisallowMultipleComponent]
    public class EnableConnectionApprovalAuthoring : MonoBehaviour
    {
        class Baker : Baker<EnableConnectionApprovalAuthoring>
        {
            public override void Bake(EnableConnectionApprovalAuthoring authoring)
            {
                EnableConnectionApproval component = default(EnableConnectionApproval);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
