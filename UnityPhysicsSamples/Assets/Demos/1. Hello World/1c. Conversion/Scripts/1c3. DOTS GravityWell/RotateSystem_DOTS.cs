using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateBefore(typeof(GravityWellSystem_DOTS))]
public class RotateSystem_DOTS : SystemBase
{
    protected override void OnCreate()
    {
        // Only need to update the system if there are any entities with the associated component.
        RequireForUpdate(GetEntityQuery(
                                typeof(Rotation),
                                ComponentType.ReadOnly<RotateComponent_DOTS>()));
    }

    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;
        Entities
        .WithBurst()
        .ForEach((ref Rotation rotation, in RotateComponent_DOTS rotator) =>
        {
            var av = rotator.LocalAngularVelocity * deltaTime;
            rotation.Value = math.mul(rotation.Value, quaternion.Euler(av));
        }).Schedule();
    }
}

