using Unity.Entities;
using UnityEngine;

namespace Samples.FixedTimestepSystem.Authoring
{
    [AddComponentMenu("DOTS Samples/FixedTimestepWorkaround/Projectile Spawn Time")]
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
