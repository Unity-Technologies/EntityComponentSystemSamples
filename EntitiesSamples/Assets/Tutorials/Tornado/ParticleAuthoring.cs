using UnityEngine;
using Unity.Entities;

namespace Tutorials.Tornado
{
    public class ParticleAuthoring : MonoBehaviour
    {
        class Baker : Baker<ParticleAuthoring>
        {
            public override void Bake(ParticleAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Particle>(entity);
            }
        }
    }

    public struct Particle : IComponentData
    {
        public float radiusMult;
    }
}
