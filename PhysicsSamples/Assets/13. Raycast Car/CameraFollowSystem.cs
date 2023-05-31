using Common.Scripts;
using Unity.Burst;
using Unity.Entities;

namespace RaycastCar
{
    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(ChangeActiveVehicleSystem))]
    partial struct CameraFollowSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CameraSmoothTrackSettings>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<CameraSmoothTrackSettings>())
            {
                return;
            }

            var cameraSettings = SystemAPI.GetSingleton<CameraSmoothTrackSettings>();

            foreach (var vehicle in
                     SystemAPI.Query<RefRO<VehicleCameraReferences>>()
                         .WithAll<ActiveVehicle>())
            {
                cameraSettings.Target = vehicle.ValueRO.CameraTarget;
                cameraSettings.LookTo = vehicle.ValueRO.CameraTo;
                cameraSettings.LookFrom = vehicle.ValueRO.CameraFrom;
            }

            SystemAPI.SetSingleton(cameraSettings);
        }
    }
}
