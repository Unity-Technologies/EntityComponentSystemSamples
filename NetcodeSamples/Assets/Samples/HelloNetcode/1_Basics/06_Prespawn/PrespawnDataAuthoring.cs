using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct PrespawnData : IComponentData
    {
        [GhostField] public int Value;

        public float Direction;
    }

    [DisallowMultipleComponent]
    public class PrespawnDataAuthoring : MonoBehaviour
    {
        [RegisterBinding(typeof(PrespawnData), "Value")]
        public int Value;
        [RegisterBinding(typeof(PrespawnData), "Direction")]
        public float Direction;

        class Baker : Baker<PrespawnDataAuthoring>
        {
            public override void Bake(PrespawnDataAuthoring authoring)
            {
                PrespawnData component = default(PrespawnData);
                component.Value = authoring.Value;
                component.Direction = authoring.Direction;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
