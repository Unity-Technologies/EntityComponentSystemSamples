using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct BarrelSpawner : IComponentData
    {
        public Entity Barrel;
        public Entity BarrelWithoutImportance;
    }

    [DisallowMultipleComponent]
    public class BarrelSpawnerAuthoring : MonoBehaviour
    {
        public GameObject Barrel;
        public GameObject BarrelNoImportance;

        class Baker : Baker<BarrelSpawnerAuthoring>
        {
            public override void Bake(BarrelSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new BarrelSpawner
                {
                    Barrel = GetEntity(authoring.Barrel, TransformUsageFlags.Dynamic),
                    BarrelWithoutImportance = GetEntity(authoring.BarrelNoImportance, TransformUsageFlags.Dynamic),
                });
            }
        }
    }
}
