using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ParticleEmitterAuthoringComponent : MonoBehaviour
{
    public float particlesPerSecond;
    public float angleSpread;
    public float velocityBase;
    public float velocityRandom;
    public float2 spawnOffset;
    public float spawnSpread;
    public float particleLifetime;

    public float startLength;
    public float startWidth;
    public float4 startColor;
    public float endLength;
    public float endWidth;
    public float4 endColor;
    public bool active;
    public GameObject particlePrefab;

    public class Baker : Baker<ParticleEmitterAuthoringComponent>
    {
        public override void Bake(ParticleEmitterAuthoringComponent authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ParticleEmitterComponentData
            {
                particlesPerSecond = authoring.particlesPerSecond,
                angleSpread = authoring.angleSpread,
                velocityBase = authoring.velocityBase,
                velocityRandom = authoring.velocityRandom,
                spawnOffset = authoring.spawnOffset,
                spawnSpread = authoring.spawnSpread,
                particleLifetime = authoring.particleLifetime,
                startLength = authoring.startLength,
                startWidth = authoring.startWidth,
                startColor = authoring.startColor,
                endLength = authoring.endLength,
                endWidth = authoring.endWidth,
                endColor = authoring.endColor,
                active = authoring.active?1:0,
                particlePrefab = GetEntity(authoring.particlePrefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}
