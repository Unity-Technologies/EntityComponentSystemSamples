using Unity.Burst;
using Unity.Entities;

[RequireMatchingQueriesForUpdate]
[UpdateAfter(typeof(ChangeActiveVehicleSystem))]
[BurstCompile]
partial struct CameraFollowActiveVehicleSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<CameraSmoothTrackSettings>())
        {
            return;
        }

        var cameraSettings = SystemAPI.GetSingleton<CameraSmoothTrackSettings>();

        foreach (var vehicle in SystemAPI.Query<RefRO<VehicleCameraReferences>>().WithAll<ActiveVehicle>())
        {
            cameraSettings.Target = vehicle.ValueRO.CameraTarget;
            cameraSettings.LookTo = vehicle.ValueRO.CameraTo;
            cameraSettings.LookFrom = vehicle.ValueRO.CameraFrom;
        }

        SystemAPI.SetSingleton(cameraSettings);
    }
}
