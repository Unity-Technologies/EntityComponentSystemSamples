using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Samples.MultyPhysicsWorld
{
    public struct ParticleEmitter : IComponentData
    {
        public float emissionRate;
        public float particleAge;
        public Entity prefab;
        public Random random;
        public float accumulator;
    }

    [DisallowMultipleComponent]
    public class ParticleEmitterAuthoring : MonoBehaviour
    {
        [RegisterBinding(typeof(ParticleEmitter), "emissionRate")]
        public float emissionRate;
        [RegisterBinding(typeof(ParticleEmitter), "particleAge")]
        public float particleAge;
        public GameObject prefab;
        [RegisterBinding(typeof(ParticleEmitter), "random")]
        public Random random;
        [RegisterBinding(typeof(ParticleEmitter), "accumulator")]
        public float accumulator;

        class ParticleEmitterBaker : Baker<ParticleEmitterAuthoring>
        {
            public override void Bake(ParticleEmitterAuthoring authoring)
            {
                ParticleEmitter component = default(ParticleEmitter);
                component.emissionRate = authoring.emissionRate;
                component.particleAge = authoring.particleAge;
                component.prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic);
                component.random = authoring.random;
                component.accumulator = authoring.accumulator;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
