using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;

public struct StaticAsteroid : IComponentData
{
    [GhostField(Quantization = 100)] public float2 InitialPosition;
    [GhostField(Quantization = 100)] public float2 InitialVelocity;
    [GhostField(Quantization = 100)] public float InitialAngle;
    [GhostField] public NetworkTick SpawnTick;

    public float3 GetPosition(NetworkTick tick, float fraction, float fixedDeltaTime)
    {
        float dt = (tick.TicksSince(SpawnTick) - (1.0f - fraction)) * fixedDeltaTime;
        return new float3(InitialPosition + InitialVelocity*dt, 0);
    }
    public quaternion GetRotation(NetworkTick tick, float fraction, float fixedDeltaTime)
    {
        float dt = (tick.TicksSince(SpawnTick) - (1.0f - fraction)) * fixedDeltaTime;
        float angle = 360.0f;
        math.modf(dt*100 + InitialAngle, out angle);
        return quaternion.RotateZ(math.radians(angle));
    }
}
