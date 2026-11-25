using Unity.Netcode;
using Unity.Entities;

using UnityEngine;

namespace Samples.HelloNetcode
{
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct GrenadeData : IComponentData
    {
        [GhostField]
        public uint SpawnId;

        public float DestroyTimer;
    }

    public class GrenadeDataAuthoring : MonoBehaviour
    {
        class Baker : Baker<GrenadeDataAuthoring>
        {
            public override void Bake(GrenadeDataAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                // Prevent predicted spawned grenades from predicting that they should be destroyed, by setting DestroyTimer to inf.
                AddComponent(entity, new GrenadeData { DestroyTimer = float.PositiveInfinity });
            }
        }
    }
}
