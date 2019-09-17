using System;
using Demos;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

struct Vehicle : IComponentData { }

struct VehicleSpeed : IComponentData
{
    public float TopSpeed;
    public float DesiredSpeed;
}

struct VehicleSteering : IComponentData
{
    public float MaxSteeringAngle;
    public float DesiredSteeringAngle;
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

// TODO: entities currently only supports component objects inheriting UnityEngine.Component
class VehicleReferences : Component
{
    public VehicleMechanics Mechanics;
    public Transform CameraOrbit;
    public Transform CameraTarget;
    public Transform CameraTo;
    public Transform CameraFrom;
}

class VehicleAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    #pragma warning disable 649
    public bool ActiveAtStart;

    public VehicleMechanics Mechanics;

    [Header("Handling")]
    public float TopSpeed = 10.0f;
    public float MaxSteeringAngle = 30.0f;

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
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (ActiveAtStart)
            dstManager.AddComponent(entity, typeof(ActiveVehicle));

        dstManager.AddComponent(entity, typeof(Vehicle));

        dstManager.AddComponent(entity, typeof(VehicleCameraSettings));
        dstManager.SetComponentData(entity, new VehicleCameraSettings
        {
            OrientationType = CameraOrientation,
            OrbitAngularSpeed = math.radians(CameraOrbitAngularSpeed)
        });

        dstManager.AddComponent(entity, typeof(VehicleSpeed));
        dstManager.SetComponentData(entity, new VehicleSpeed { TopSpeed = TopSpeed });

        dstManager.AddComponent(entity, typeof(VehicleSteering));
        dstManager.SetComponentData(entity, new VehicleSteering { MaxSteeringAngle = MaxSteeringAngle });

        dstManager.AddComponentObject(entity, new VehicleReferences
        {
            Mechanics = Mechanics,
            CameraOrbit = CameraOrbit,
            CameraTarget = CameraTarget,
            CameraTo = CameraTo,
            CameraFrom = CameraFrom
        });
    }
}
