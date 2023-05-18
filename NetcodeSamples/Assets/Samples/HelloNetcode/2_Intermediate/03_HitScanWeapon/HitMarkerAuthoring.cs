using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct ServerHitMarker : IComponentData
    {
        [GhostField] public Entity Entity;
        [GhostField] public float3 HitPoint;
        [GhostField] public NetworkTick ServerHitTick;
        public NetworkTick AppliedClientTick;
    }

    public struct ClientHitMarker : IComponentData
    {
        public Entity Entity;
        public float3 HitPoint;
        public NetworkTick ClientHitTick;
        public NetworkTick AppliedClientTick;
    }

    public class HitMarkerAuthoring : MonoBehaviour
    {
        public class Baker : Baker<HitMarkerAuthoring>
        {
            public override void Bake(HitMarkerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<ServerHitMarker>(entity);
                AddComponent<ClientHitMarker>(entity);
            }
        }
    }
}
