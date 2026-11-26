using Unity.Netcode;
using Unity.Entities;

using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct PrespawnChildData : IComponentData
    {
        [GhostField] public int Value;
    }

    [DisallowMultipleComponent]
    public class PrespawnChildDataAuthoring : MonoBehaviour
    {
        [RegisterBinding(typeof(PrespawnChildData), "Value")]
        public int Value;

        class Baker : Baker<PrespawnChildDataAuthoring>
        {
            public override void Bake(PrespawnChildDataAuthoring authoring)
            {
                PrespawnChildData component = default(PrespawnChildData);
                component.Value = authoring.Value;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
