using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace RaycastCar
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    partial struct InputSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VehicleInput>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var input = SystemAPI.GetSingleton<VehicleInput>();

            foreach (var(speed, steering, cameraSettings, references)
                     in SystemAPI
                         .Query<RefRW<VehicleSpeed>, RefRW<VehicleSteering>, RefRO<VehicleCameraSettings>,
                                RefRO<VehicleCameraReferences>>().WithAll<ActiveVehicle>())
            {
                float x = input.Steering.x;
                float a = input.Throttle;
                float z = input.Looking.x;

                var newSpeed = a * speed.ValueRW.TopSpeed;
                speed.ValueRW.DriveEngaged = (byte)(newSpeed == 0f ? 0 : 1);
                speed.ValueRW.DesiredSpeed = math.lerp(speed.ValueRW.DesiredSpeed, newSpeed, speed.ValueRW.Damping);

                var newSteeringAngle = x * steering.ValueRW.MaxSteeringAngle;
                steering.ValueRW.DesiredSteeringAngle = math.lerp(steering.ValueRW.DesiredSteeringAngle,
                    newSteeringAngle, steering.ValueRW.Damping);


                if (SystemAPI.HasComponent<LocalTransform>(references.ValueRO.CameraOrbit))
                {
                    var localTransform = SystemAPI.GetComponent<LocalTransform>(references.ValueRO.CameraOrbit);
                    switch (cameraSettings.ValueRO.OrientationType)
                    {
                        case VehicleCameraOrientation.Relative:
                            localTransform.Rotation = math.mul(localTransform.Rotation,
                                quaternion.Euler(0f,
                                    z * SystemAPI.Time.DeltaTime * cameraSettings.ValueRO.OrbitAngularSpeed, 0f));
                            break;
                        case VehicleCameraOrientation.Absolute:
                            float4x4 worldFromLocal =
                                SystemAPI.HasComponent<LocalToWorld>(references.ValueRO.CameraOrbit)
                                ? SystemAPI.GetComponent<LocalToWorld>(references.ValueRO.CameraOrbit).Value
                                : float4x4.identity;
                            float4x4 worldFromParent = SystemAPI.HasComponent<Parent>(references.ValueRO.CameraOrbit)
                                ? math.mul(worldFromLocal, math.inverse(localTransform.ToMatrix()))
                                : worldFromLocal;
                            var worldOrientation = quaternion.Euler(0f, z * math.PI, 0f);
                            localTransform.Rotation =
                                math.mul(new quaternion(math.orthonormalize(new float3x3(worldFromParent))),
                                    worldOrientation);
                            break;
                    }

                    SystemAPI.SetComponent(references.ValueRO.CameraOrbit, localTransform);
                }
            }
        }
    }

    struct VehicleInput : IComponentData
    {
        public float2 Looking;
        public float2 Steering;
        public float Throttle;
        public int Change; // positive to change to a subsequent vehicle, negative to change to a previous one
    }
}
