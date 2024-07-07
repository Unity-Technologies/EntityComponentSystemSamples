using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Samples.HelloNetcode
{
    /// <summary>
    /// Component added to the attacker, replicating server-confirmed shot results (thus, a max of one hit per frame).
    /// </summary>
    public struct ServerHitMarker : IComponentData
    {
        [GhostField] public Entity Victim;
        [GhostField] public float3 HitPoint;
        [GhostField] public NetworkTick ServerHitTick;
        public NetworkTick AppliedClientTick;
    }

    /// <summary>
    /// Similar to <see cref="ServerHitMarker"/>, but storing client predicted shot results.
    /// Thus, not replicated via <see cref="GhostFieldAttribute"/>.
    /// </summary>
    public struct ClientHitMarker : IComponentData
    {
        public Entity Victim;
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
