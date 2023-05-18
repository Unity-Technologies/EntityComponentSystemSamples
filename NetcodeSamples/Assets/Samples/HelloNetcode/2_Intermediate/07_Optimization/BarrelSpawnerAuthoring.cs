using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct BarrelSpawner : IComponentData
    {
        public Entity Barrel;
    }

    [DisallowMultipleComponent]
    public class BarrelSpawnerAuthoring : MonoBehaviour
    {
        public GameObject Barrel;

        class Baker : Baker<BarrelSpawnerAuthoring>
        {
            public override void Bake(BarrelSpawnerAuthoring authoring)
            {
                BarrelSpawner component = default(BarrelSpawner);
                component.Barrel = GetEntity(authoring.Barrel, TransformUsageFlags.Dynamic);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
