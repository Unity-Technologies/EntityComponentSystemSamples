using Unity.Entities;

[UpdateAfter(typeof(ChangeActiveVehicleSystem))]
partial class CameraFollowActiveVehicleSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (!HasSingleton<CameraSmoothTrackSettings>())
            return;

        var cameraSettings = GetSingleton<CameraSmoothTrackSettings>();

        Entities
            .WithName("CameraFollowActiveVehicleJob")
            .WithBurst()
            .WithAll<ActiveVehicle>()
            .ForEach((in VehicleCameraReferences vehicle) =>
            {
                cameraSettings.Target = vehicle.CameraTarget;
                cameraSettings.LookTo = vehicle.CameraTo;
                cameraSettings.LookFrom = vehicle.CameraFrom;
            }).Run();

        SetSingleton(cameraSettings);
    }
}
