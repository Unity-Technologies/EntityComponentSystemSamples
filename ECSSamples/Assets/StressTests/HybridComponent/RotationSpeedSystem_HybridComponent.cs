using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// ReSharper disable once InconsistentNaming
public class RotationSpeedSystem_HybridComponent : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref RotationSpeed_HybridComponent rotationSpeed, ref Rotation rotation) =>
        {
            var deltaTime = Time.DeltaTime;
            rotation.Value = math.mul(math.normalize(rotation.Value),
                quaternion.AxisAngle(math.up(), rotationSpeed.RadiansPerSecond * deltaTime));
        });
    }
}
