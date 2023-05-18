using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct EnableImportance : IComponentData
    {
    }

    public class EnableImportanceAuthoring : MonoBehaviour
    {
        class Baker : Baker<EnableImportanceAuthoring>
        {
            public override void Bake(EnableImportanceAuthoring authoring)
            {
                EnableImportance component = default(EnableImportance);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}

