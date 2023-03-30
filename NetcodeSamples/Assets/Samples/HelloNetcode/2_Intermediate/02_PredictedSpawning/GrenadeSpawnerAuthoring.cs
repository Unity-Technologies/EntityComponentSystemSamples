using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct GrenadeSpawner : IComponentData
    {
        public Entity Grenade;
        public Entity Explosion;
    }

    [DisallowMultipleComponent]
    public class GrenadeSpawnerAuthoring : MonoBehaviour
    {
        public GameObject Grenade;
        public GameObject Explosion;

        class Baker : Baker<GrenadeSpawnerAuthoring>
        {
            public override void Bake(GrenadeSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new GrenadeSpawner
                {
                    Grenade = GetEntity(authoring.Grenade, TransformUsageFlags.Dynamic),
                    Explosion = GetEntity(authoring.Explosion, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}
