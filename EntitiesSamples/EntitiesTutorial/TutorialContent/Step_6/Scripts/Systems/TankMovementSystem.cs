using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial class TankMovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var dt = SystemAPI.Time.DeltaTime;

        Entities
            .WithAll<Tank>()
            .ForEach((Entity entity, TransformAspect transform) =>
            {
                var pos = transform.LocalPosition;

                // This does not modify the actual position of the tank, only the point at
                // which we sample the 3D noise function. This way, every tank is using a
                // different slice and will move along its own different random flow field.
                pos.y = entity.Index;
                var angle = (0.5f + noise.cnoise(pos / 10f)) * 4.0f * math.PI;

                var dir = float3.zero;
                math.sincos(angle, out dir.x, out dir.z);
                transform.LocalPosition += dir * dt * 5.0f;
                transform.LocalRotation = quaternion.RotateY(angle);
            }).ScheduleParallel();
    }
}
