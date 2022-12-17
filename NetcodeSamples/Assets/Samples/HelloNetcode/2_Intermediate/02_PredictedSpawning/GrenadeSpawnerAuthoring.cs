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
                AddComponent(new GrenadeSpawner {Grenade = GetEntity(authoring.Grenade), Explosion = GetEntity(authoring.Explosion)});
            }
        }
    }
}
