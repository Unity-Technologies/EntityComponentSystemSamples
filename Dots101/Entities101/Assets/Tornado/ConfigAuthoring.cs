using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tutorials.Tornado
{
    public class ConfigAuthoring : MonoBehaviour
    {
        public GameObject BarPrefab;
        [Range(0f, 1f)] public float BarDamping;
        [Range(0f, 1f)] public float BarFriction;
        public float BarBreakResistance;

        [Range(0f, 1f)] public float TornadoForce;
        public float TornadoMaxForceDist;
        public float TornadoHeight;
        public float TornadoUpForce;
        public float TornadoInwardForce;

        public GameObject ParticlePrefab;
        public float ParticleSpinRate;
        public float ParticleUpwardSpeed;

        class Baker : Baker<ConfigAuthoring>
        {
            public override void Bake(ConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Config
                {
                    BarPrefab = GetEntity(authoring.BarPrefab, TransformUsageFlags.Dynamic),
                    BarDamping = authoring.BarDamping,
                    BarFriction = authoring.BarFriction,
                    BarBreakResistance = authoring.BarBreakResistance,

                    TornadoForce = authoring.TornadoForce,
                    TornadoMaxForceDist = authoring.TornadoMaxForceDist,
                    TornadoHeight = authoring.TornadoHeight,
                    TornadoUpForce = authoring.TornadoUpForce,
                    TornadoInwardForce = authoring.TornadoInwardForce,

                    ParticlePrefab = GetEntity(authoring.ParticlePrefab, TransformUsageFlags.Dynamic),
                    ParticleSpinRate = authoring.ParticleSpinRate,
                    ParticleUpwardSpeed = authoring.ParticleUpwardSpeed
                });
            }
        }
    }

    public struct Config : IComponentData
    {
        public Entity BarPrefab;
        public float BarDamping;
        public float BarFriction;
        public float BarBreakResistance;

        public float TornadoForce;
        public float TornadoMaxForceDist;
        public float TornadoHeight;
        public float TornadoUpForce;
        public float TornadoInwardForce;

        public Entity ParticlePrefab;
        public float ParticleSpinRate;
        public float ParticleUpwardSpeed;
    }
}
