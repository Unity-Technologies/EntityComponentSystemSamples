using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

struct Vehicle : IComponentData {}

struct VehicleSpeed : IComponentData
{
    public float TopSpeed;
    public float DesiredSpeed;
    public float Damping;
    public byte DriveEngaged;
}

struct VehicleSteering : IComponentData
{
    public float MaxSteeringAngle;
    public float DesiredSteeringAngle;
    public float Damping;
}

enum VehicleCameraOrientation
{
    Absolute,
    Relative
}

struct VehicleCameraSettings : IComponentData
{
    public VehicleCameraOrientation OrientationType;
    public float OrbitAngularSpeed;
}

struct VehicleCameraReferences : IComponentData
{
    public Entity CameraOrbit;
    public Entity CameraTarget;
    public Entity CameraTo;
    public Entity CameraFrom;
}

class VehicleAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    #pragma warning disable 649
    public bool ActiveAtStart;

    [Header("Handling")]
    public float TopSpeed = 10.0f;
    public float MaxSteeringAngle = 30.0f;
    [Range(0f, 1f)] public float SteeringDamping = 0.1f;
    [Range(0f, 1f)] public float SpeedDamping = 0.01f;

    [Header("Camera Settings")]
    public Transform CameraOrbit;
    public VehicleCameraOrientation CameraOrientation = VehicleCameraOrientation.Relative;
    public float CameraOrbitAngularSpeed = 180f;
    public Transform CameraTarget;
    public Transform CameraTo;
    public Transform CameraFrom;
    #pragma warning restore 649

    void OnValidate()
    {
        TopSpeed = math.max(0f, TopSpeed);
        MaxSteeringAngle = math.max(0f, MaxSteeringAngle);
        SteeringDamping = math.clamp(SteeringDamping, 0f, 1f);
        SpeedDamping = math.clamp(SpeedDamping, 0f, 1f);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (ActiveAtStart)
            dstManager.AddComponent<ActiveVehicle>(entity);

        dstManager.AddComponent<Vehicle>(entity);

        dstManager.AddComponentData(entity, new VehicleCameraSettings
        {
            OrientationType = CameraOrientation,
            OrbitAngularSpeed = math.radians(CameraOrbitAngularSpeed)
        });

        dstManager.AddComponentData(entity, new VehicleSpeed
        {
            TopSpeed = TopSpeed,
            Damping = SpeedDamping
        });

        dstManager.AddComponentData(entity, new VehicleSteering
        {
            MaxSteeringAngle = math.radians(MaxSteeringAngle),
            Damping = SteeringDamping
        });

        dstManager.AddComponentData(entity, new VehicleCameraReferences
        {
            CameraOrbit = conversionSystem.GetPrimaryEntity(CameraOrbit),
            CameraTarget = conversionSystem.GetPrimaryEntity(CameraTarget),
            CameraTo = conversionSystem.GetPrimaryEntity(CameraTo),
            CameraFrom = conversionSystem.GetPrimaryEntity(CameraFrom)
        });
    }
}
