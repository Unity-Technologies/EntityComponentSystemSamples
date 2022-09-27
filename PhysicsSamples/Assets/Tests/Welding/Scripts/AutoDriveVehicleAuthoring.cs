using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Demos
{
    [RequireComponent(typeof(VehicleAuthoring))]
    class AutoDriveVehicleAuthoring : MonoBehaviour
    {
        public float DesiredSpeed = 40f;
        public float DesiredSteeringAngle = 10f;

        class AutoDriveVehicleBaker : Baker<AutoDriveVehicleAuthoring>
        {
            public override void Bake(AutoDriveVehicleAuthoring authoring)
            {
                var vehicle = GetComponent<VehicleAuthoring>();

                AddComponent(new VehicleInputOverride
                {
                    Value = new VehicleInput
                    {
                        Steering = new float2(authoring.DesiredSteeringAngle / vehicle.MaxSteeringAngle, 0f),
                        Throttle = authoring.DesiredSpeed / vehicle.TopSpeed
                    }
                });
            }
        }
    }

    struct VehicleInputOverride : IComponentData
    {
        public VehicleInput Value;
    }

    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(DemoInputGatheringSystem))]
    [UpdateBefore(typeof(VehicleInputHandlingSystem))]
    partial class AutoDriveVehicle : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!HasSingleton<VehicleInputOverride>())
                return;

            var input = GetSingleton<VehicleInput>();
            input = GetSingleton<VehicleInputOverride>().Value;
            SetSingleton(input);
        }
    }
}
