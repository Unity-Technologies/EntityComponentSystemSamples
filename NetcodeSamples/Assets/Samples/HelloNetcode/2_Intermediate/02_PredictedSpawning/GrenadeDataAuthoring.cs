using Unity.Entities;
using Unity.NetCode;
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
                AddComponent<GrenadeData>(entity);
            }
        }
    }
}
