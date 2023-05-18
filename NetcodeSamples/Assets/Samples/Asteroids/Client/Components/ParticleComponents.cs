using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

public struct Particle : IComponentData
{}

public struct ParticleAge : IComponentData
{
    public ParticleAge(float maxAge)
    {
        this.maxAge = maxAge;
        age = 0;
    }

    public float maxAge;
    public float age;
}

public struct ParticleVelocity : IComponentData
{
    public ParticleVelocity(float2 velocity)
    {
        this.velocity = velocity;
    }

    public float2 velocity;
}

public struct ParticleColorTransition : IComponentData
{
    public ParticleColorTransition(float4 start, float4 end)
    {
        startColor = start;
        endColor = end;
    }

    public float4 startColor;
    public float4 endColor;
}

public struct ParticleSizeTransition : IComponentData
{
    public ParticleSizeTransition(float startL, float startW, float endL, float endW)
    {
        startLength = startL;
        startWidth = startW;
        endLength = endL;
        endWidth = endW;
    }

    public float startLength;
    public float startWidth;
    public float endLength;
    public float endWidth;
}
