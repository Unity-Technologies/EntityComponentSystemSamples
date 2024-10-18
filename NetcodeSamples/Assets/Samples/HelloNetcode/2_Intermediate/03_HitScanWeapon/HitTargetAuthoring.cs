using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct HitTarget : IComponentData
    {
        public float Speed;
        public float MovingRange;
    }

    public class HitTargetAuthoring : MonoBehaviour
    {
        public float Speed = 5f;
        public float MovingRange = 10f;

        public class Baker : Baker<HitTargetAuthoring>
        {
            public override void Bake(HitTargetAuthoring authoring)
            {
                var component = default(HitTarget);
                component.Speed = authoring.Speed;
                component.MovingRange = authoring.MovingRange;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
