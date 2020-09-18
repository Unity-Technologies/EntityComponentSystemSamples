using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Demos
{
    [RequireComponent(typeof(VehicleAuthoring))]
    class AutoDriveVehicleAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float DesiredSpeed = 40f;
        public float DesiredSteeringAngle = 10f;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var vehicle = GetComponent<VehicleAuthoring>();

            dstManager.AddComponent(entity, typeof(VehicleInputOverride));
            dstManager.SetComponentData(entity, new VehicleInputOverride
            {
                Value = new VehicleInput
                {
                    Steering = new float2(DesiredSteeringAngle / vehicle.MaxSteeringAngle, 0f),
                    Throttle = DesiredSpeed / vehicle.TopSpeed
                }
            });
        }
    }

    struct VehicleInputOverride : IComponentData
    {
        public VehicleInput Value;
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(DemoInputGatheringSystem))]
    [UpdateBefore(typeof(VehicleInputHandlingSystem))]
    class AutoDriveVehicle : SystemBase
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
