using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;

public struct StaticAsteroid : IComponentData
{
    [GhostField(Quantization = 100)] public float2 InitialPosition;
    [GhostField(Quantization = 100)] public float2 InitialVelocity;
    [GhostField(Quantization = 100)] public float InitialAngle;
    [GhostField] public NetworkTick SpawnTick;

    public readonly float3 GetPosition(NetworkTick tick, float fraction, float fixedDeltaTime)
    {
        float dt = (tick.TicksSince(SpawnTick) - (1.0f - fraction)) * fixedDeltaTime;
        return new float3(InitialPosition + InitialVelocity*dt, 0);
    }
    public readonly quaternion GetRotation(NetworkTick tick, float fraction, float fixedDeltaTime)
    {
        float dt = (tick.TicksSince(SpawnTick) - (1.0f - fraction)) * fixedDeltaTime;
        math.modf(dt*100 + InitialAngle, out var angle);
        return quaternion.RotateZ(math.radians(angle));
    }
}
