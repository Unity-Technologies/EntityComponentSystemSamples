using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct RotateEntityJob : IJobEntity
{
    public float DeltaTime;

    public void Execute(ref Rotation rotation, in RotationSpeed_IJobEntity speed)
    {
        rotation.Value =
            math.mul(
                math.normalize(rotation.Value),
                quaternion.AxisAngle(math.up(), speed.RadiansPerSecond * DeltaTime));
    }
}
public partial class RotationSpeedSystem_IJobEntity : SystemBase
{
    // OnUpdate runs on the main thread.
    protected override void OnUpdate()
    {
        new RotateEntityJob {DeltaTime = Time.DeltaTime}.Schedule();
    }
}
