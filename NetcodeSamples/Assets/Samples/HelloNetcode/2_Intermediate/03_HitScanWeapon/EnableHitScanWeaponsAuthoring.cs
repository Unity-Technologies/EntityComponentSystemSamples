using UnityEngine;
using Unity.Entities;

namespace Samples.HelloNetcode
{
    public struct EnableHitScanWeapons : IComponentData
    {
    }

    public class EnableHitScanWeaponsAuthoring : MonoBehaviour
    {
        class Baker : Baker<EnableHitScanWeaponsAuthoring>
        {
            public override void Bake(EnableHitScanWeaponsAuthoring authoring)
            {
                AddComponent<EnableHitScanWeapons>();
            }
        }
    }
}
