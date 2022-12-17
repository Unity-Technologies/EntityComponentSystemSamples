using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct AnchorPoint : IComponentData
    {
        public Entity SpawnPoint;
        public Entity WeaponSlot;
    }

    public class AnchorPointAuthoring : MonoBehaviour
    {
        public GameObject SpawnPoint;
        public GameObject WeaponSlot;

        class Baker : Baker<AnchorPointAuthoring>
        {
            public override void Bake(AnchorPointAuthoring authoring)
            {
                AddComponent(new AnchorPoint {SpawnPoint = GetEntity(authoring.SpawnPoint), WeaponSlot = GetEntity(authoring.WeaponSlot)});
            }
        }
    }
}
