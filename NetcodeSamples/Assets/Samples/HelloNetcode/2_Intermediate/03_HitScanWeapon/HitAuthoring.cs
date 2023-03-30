using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct Hit : IComponentData
    {
        public Entity Entity;
        public NetworkTick Tick;
        public float3 HitPoint;
    }

    public class HitAuthoring : MonoBehaviour
    {
        class Baker : Baker<HitAuthoring>
        {
            public override void Bake(HitAuthoring authoring)
            {
                Hit component = default(Hit);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}

