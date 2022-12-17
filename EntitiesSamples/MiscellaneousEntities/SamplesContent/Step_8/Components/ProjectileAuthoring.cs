using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Samples.FixedTimestep
{
    public struct Projectile : IComponentData
    {
        public float SpawnTime;
        public float3 SpawnPos;
    }

    public class ProjectileAuthoring : MonoBehaviour
    {
        class Baker : Baker<ProjectileAuthoring>
        {
            public override void Bake(ProjectileAuthoring authoring)
            {
                AddComponent<Projectile>();
            }
        }
    }
}
