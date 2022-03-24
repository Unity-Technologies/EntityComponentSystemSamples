using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// This struct system updates all entities in the scene with both a RotationSpeed_ForEach_ISystem and Rotation component.
[BurstCompile]
public partial struct RotationSpeedSystemForEachISystem : ISystem
{
    public void OnCreate(ref SystemState state) {}
    public void OnDestroy(ref SystemState state) {}

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = state.Time.DeltaTime;

        // Schedule job to rotate around up vector (10k times for scale)
        for (var i = 0; i < 10000; i++)
        {
            state.Entities
                .WithName("RotationSpeedSystemForEachISystem")
                .ForEach((ref Rotation rotation, in RotationSpeed_ForEach_ISystem rotationSpeed) =>
                {
                    rotation.Value = math.mul(
                        math.normalize(rotation.Value),
                        quaternion.AxisAngle(math.up(), rotationSpeed.RadiansPerSecond * deltaTime));
                })
                .ScheduleParallel();
        }
    }
}
