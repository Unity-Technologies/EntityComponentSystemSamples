using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;

namespace RaycastCar
{
    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(EndColliderBakingSystem))]
    [UpdateAfter(typeof(PhysicsBodyBakingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    partial struct VehicleMechanicsBakingSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var(m, vehicleEntity)
                     in SystemAPI.Query<RefRO<VehicleMechanicsForBaking>>().WithEntityAccess()
                         .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities))
            {
                foreach (var wheel in m.ValueRO.Wheels)
                {
                    var wheelEntity = wheel.Wheel;

                    // Assumed hierarchy:
                    // - chassis
                    //  - mechanics
                    //   - suspension
                    //    - wheel (rotates about yaw axis and translates along suspension up)
                    //     - graphic (rotates about pitch axis)

                    RigidTransform worldFromSuspension = wheel.WorldFromSuspension;
                    RigidTransform worldFromChassis = wheel.WorldFromChassis;

                    var chassisFromSuspension = math.mul(math.inverse(worldFromChassis), worldFromSuspension);

                    ecb.AddComponent(wheelEntity, new Wheel
                    {
                        Vehicle = vehicleEntity,
                        GraphicalRepresentation =
                            wheel.GraphicalRepresentation, // assume wheel has a single child with rotating graphic
                        // TODO assume for now that driving/steering wheels also appear in this list
                        UsedForSteering = (byte)(m.ValueRO.steeringWheels.Contains(wheelEntity) ? 1 : 0),
                        UsedForDriving = (byte)(m.ValueRO.driveWheels.Contains(wheelEntity) ? 1 : 0),
                        ChassisFromSuspension = chassisFromSuspension
                    });
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
