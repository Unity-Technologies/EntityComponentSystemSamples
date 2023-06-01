using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace RaycastCar
{
    [RequireComponent(typeof(PhysicsBodyAuthoring))]
    public class VehicleMechanicsAuthoring : MonoBehaviour
    {
        [Header("Wheel Parameters...")] public List<GameObject> wheels = new List<GameObject>();
        public float wheelBase = 0.5f;
        public float wheelFrictionRight = 0.5f;
        public float wheelFrictionForward = 0.5f;
        public float wheelMaxImpulseRight = 10.0f;
        public float wheelMaxImpulseForward = 10.0f;
        [Header("Suspension Parameters...")] public float suspensionLength = 0.5f;
        public float suspensionStrength = 1.0f;
        public float suspensionDamping = 0.1f;
        [Header("Steering Parameters...")] public List<GameObject> steeringWheels = new List<GameObject>();
        [Header("Drive Parameters...")] public List<GameObject> driveWheels = new List<GameObject>();

        [Header("Miscellaneous Parameters...")]
        public bool drawDebugInformation = false;

        class Baker : Baker<VehicleMechanicsAuthoring>
        {
            public override void Bake(VehicleMechanicsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<VehicleBody>(entity);
                AddComponent(entity, new VehicleConfiguration
                {
                    wheelBase = authoring.wheelBase,
                    wheelFrictionRight = authoring.wheelFrictionRight,
                    wheelFrictionForward = authoring.wheelFrictionForward,
                    wheelMaxImpulseRight = authoring.wheelMaxImpulseRight,
                    wheelMaxImpulseForward = authoring.wheelMaxImpulseForward,
                    suspensionLength = authoring.suspensionLength,
                    suspensionStrength = authoring.suspensionStrength,
                    suspensionDamping = authoring.suspensionDamping,
                    invWheelCount = 1f / authoring.wheels.Count,
                    drawDebugInformation = (byte)(authoring.drawDebugInformation ? 1 : 0)
                });
                AddComponent(entity, new VehicleMechanicsForBaking()
                {
                    Wheels = GetWheelInfo(authoring.wheels, Allocator.Temp),
                    steeringWheels = ToNativeArray(authoring.steeringWheels, Allocator.Temp),
                    driveWheels = ToNativeArray(authoring.driveWheels, Allocator.Temp)
                });
            }

            NativeArray<WheelBakingInfo> GetWheelInfo(List<GameObject> wheels, Allocator allocator)
            {
                if (wheels == null)
                {
                    return default;
                }

                var array = new NativeArray<WheelBakingInfo>(wheels.Count, allocator);
                int i = 0;
                foreach (var wheel in wheels)
                {
                    RigidTransform worldFromSuspension = new RigidTransform
                    {
                        pos = wheel.transform.parent.position,
                        rot = wheel.transform.parent.rotation
                    };

                    RigidTransform worldFromChassis = new RigidTransform
                    {
                        pos = wheel.transform.parent.parent.parent.position,
                        rot = wheel.transform.parent.parent.parent.rotation
                    };

                    array[i++] = new WheelBakingInfo()
                    {
                        Wheel = GetEntity(wheel, TransformUsageFlags.Dynamic),
                        GraphicalRepresentation = GetEntity(wheel.transform.GetChild(0), TransformUsageFlags.Dynamic),
                        WorldFromSuspension = worldFromSuspension,
                        WorldFromChassis = worldFromChassis,
                    };
                }

                return array;
            }

            NativeArray<Entity> ToNativeArray(List<GameObject> list, Allocator allocator)
            {
                if (list == null)
                {
                    return default;
                }

                var array = new NativeArray<Entity>(list.Count, allocator);
                for (int i = 0; i < list.Count; ++i)
                {
                    array[i] = GetEntity(list[i], TransformUsageFlags.Dynamic);
                }

                return array;
            }
        }
    }

    struct WheelBakingInfo
    {
        public Entity Wheel;
        public Entity GraphicalRepresentation;
        public RigidTransform WorldFromSuspension;
        public RigidTransform WorldFromChassis;
    }

    [TemporaryBakingType]
    struct VehicleMechanicsForBaking : IComponentData
    {
        public NativeArray<WheelBakingInfo> Wheels;
        public NativeArray<Entity> steeringWheels;
        public NativeArray<Entity> driveWheels;
    }


    // configuration properties of the vehicle mechanics, which change with low frequency at run-time
    struct VehicleConfiguration : IComponentData
    {
        public float wheelBase;
        public float wheelFrictionRight;
        public float wheelFrictionForward;
        public float wheelMaxImpulseRight;
        public float wheelMaxImpulseForward;
        public float suspensionLength;
        public float suspensionStrength;
        public float suspensionDamping;
        public float invWheelCount;
        public byte drawDebugInformation;
    }

    // physics properties of the vehicle rigid body, which change with high frequency at run-time
    struct VehicleBody : IComponentData
    {
        public float SlopeSlipFactor;
        public float3 WorldCenterOfMass;
    }

    struct Wheel : IComponentData
    {
        public Entity Vehicle;
        public Entity GraphicalRepresentation;
        public byte UsedForSteering;
        public byte UsedForDriving;
        public RigidTransform ChassisFromSuspension;
    }
}
