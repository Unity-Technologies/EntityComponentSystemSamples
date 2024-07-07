using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Samples.HelloNetcode
{
    /// <summary>
    /// Stores the raw last hit data from the <see cref="ShootingSystem"/>, to be processed into either
    /// <see cref="ClientHitMarker"/> or <see cref="ServerHitMarker"/> by <see cref="ApplyHitMarkSystem"/>.
    /// </summary>
    public struct Hit : IComponentData
    {
        public Entity Victim;
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

