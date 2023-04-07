using System.Numerics;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public partial class MoveSpeedSystem_ForEach : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;
        Entities
            .WithName("MoveSpeedSystem_ForEach")
            .ForEach((ref Translation translation, in Rotation rot,in MoveSpeed_ForEach moveSpeed) =>
            {
                translation.Value = translation.Value + (float3)moveSpeed.MoveSpeed * deltaTime;
            }).ScheduleParallel();

    }
}
