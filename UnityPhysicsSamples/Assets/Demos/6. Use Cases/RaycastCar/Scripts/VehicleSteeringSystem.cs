using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

struct VehicleInput : IComponentData
{
    public float2 Looking;
    public float2 Steering;
    public float Throttle;
    public int Change; // positive to change to a subsequent vehicle, negative to change to a previous one
}

class VehicleSteeringSystem : ComponentSystem
{
    EntityQuery m_VehicleInputQuery;

    protected override void OnCreate() => m_VehicleInputQuery = GetEntityQuery(typeof(VehicleInput));

    protected override void OnUpdate()
    {
        var input = m_VehicleInputQuery.GetSingleton<VehicleInput>();

        Entities.WithAllReadOnly<ActiveVehicle>().ForEach(
            (VehicleReferences references, ref VehicleSpeed speed, ref VehicleSteering steering, ref VehicleCameraSettings cameraSettings) =>
            {
                if (references.Mechanics == null)
                    return;

                float x = input.Steering.x;
                float a = input.Throttle;
                float z = input.Looking.x;

                speed.DesiredSpeed = a * speed.TopSpeed;
                steering.DesiredSteeringAngle = x * steering.MaxSteeringAngle;

                if (references.CameraOrbit != null)
                {
                    switch (cameraSettings.OrientationType)
                    {
                        case VehicleCameraOrientation.Relative:
                            references.CameraOrbit.transform.rotation *= quaternion.Euler(0, z * UnityEngine.Time.deltaTime * cameraSettings.OrbitAngularSpeed, 0);
                            break;
                        case VehicleCameraOrientation.Absolute:
                            references.CameraOrbit.transform.rotation = quaternion.Euler(0f, z * math.PI, 0f);
                            break;
                    }
                }

                var m = references.Mechanics;
                // TODO: expose steering/speed damping to authoring component
                m.steeringAngle = Mathf.Lerp(m.steeringAngle, steering.DesiredSteeringAngle, 0.1f);
                m.driveDesiredSpeed = Mathf.Lerp(m.driveDesiredSpeed, speed.DesiredSpeed, 0.01f);
                m.driveEngaged = 0.0f != speed.DesiredSpeed;
            });
    }
}
