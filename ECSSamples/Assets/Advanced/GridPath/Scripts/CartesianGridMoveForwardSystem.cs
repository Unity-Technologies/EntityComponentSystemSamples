using System;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateBefore(typeof(TransformSystemGroup))]
public class CartesianGridMoveForwardSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle lastJobHandle)
    {
        // Clamp delta time so you can't overshoot.
        var deltaTime = math.min(Time.DeltaTime, 0.05f);

        // Move forward along direction in grid-space given speed.
        // - This is the same for Plane or Cube and is the core of the movement code. Simply "move forward" along direction.
        lastJobHandle = Entities
            .WithName("CartesianGridMoveForward")
            .ForEach((ref Translation translation,
                in CartesianGridDirection gridDirection,
                in CartesianGridSpeed gridSpeed,
                in CartesianGridCoordinates gridPosition) =>
            {
                var dir = gridDirection.Value;
                var pos = translation.Value;

                // Speed adjusted to float m/s from fixed point 6:10 m/s
                var speed = deltaTime * ((float)gridSpeed.Value) * (1.0f / 1024.0f);

                // Write: add unit vector offset scaled by speed and deltaTime to current position
                var dx = CartesianGridMovement.UnitMovement[(dir * 2) + 0] * speed;
                var dz = CartesianGridMovement.UnitMovement[(dir * 2) + 1] * speed;
                
                // Smooth y changes when transforming between cube faces. 
                var dy = math.min(speed, 1.0f - pos.y); 

                translation.Value = new float3(pos.x + dx, pos.y + dy, pos.z + dz);
            }).Schedule(lastJobHandle);
        
        return lastJobHandle;
    }
}