using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[RequireMatchingQueriesForUpdate]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial class CartesianGridMoveForwardSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Clamp delta time so you can't overshoot.
        var deltaTime = math.min(SystemAPI.Time.DeltaTime, 0.05f);

        // Move forward along direction in grid-space given speed.
        // - This is the same for Plane or Cube and is the core of the movement code. Simply "move forward" along direction.
        Entities
            .WithName("CartesianGridMoveForward")
#if !ENABLE_TRANSFORM_V1
            .ForEach((ref LocalToWorldTransform transform,
#else
            .ForEach((ref Translation translation,
#endif
                in CartesianGridDirection gridDirection,
                in CartesianGridSpeed gridSpeed,
                in CartesianGridCoordinates gridPosition) =>
                {
                    var dir = gridDirection.Value;
                    if (dir == 0xff)
                        return;

#if !ENABLE_TRANSFORM_V1
                    var pos = transform.Value.Position;
#else
                    var pos = translation.Value;
#endif

                    // Speed adjusted to float m/s from fixed point 6:10 m/s
                    var speed = deltaTime * ((float)gridSpeed.Value) * (1.0f / 1024.0f);

                    // Write: add unit vector offset scaled by speed and deltaTime to current position
                    var dx = CartesianGridMovement.UnitMovement[(dir * 2) + 0] * speed;
                    var dz = CartesianGridMovement.UnitMovement[(dir * 2) + 1] * speed;

                    // Smooth y changes when transforming between cube faces.
                    var dy = math.min(speed, 1.0f - pos.y);

#if !ENABLE_TRANSFORM_V1
                    transform.Value.Position = new float3(pos.x + dx, pos.y + dy, pos.z + dz);
#else
                    translation.Value = new float3(pos.x + dx, pos.y + dy, pos.z + dz);
#endif
                }).Schedule();
    }
}
