using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace RaycastCar
{
    [RequireMatchingQueriesForUpdate]
    partial struct ChangeActiveVehicleSystem : ISystem
    {
        struct AvailableVehicle : ICleanupComponentData
        {
        }

        NativeList<Entity> allVehicles;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            allVehicles = new NativeList<Entity>(Allocator.Persistent);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            allVehicles.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var activeVehicleQuery = SystemAPI.QueryBuilder().WithAll<ActiveVehicle, Vehicle>().Build();
            var vehicleInputQuery = SystemAPI.QueryBuilder().WithAll<VehicleInput>().Build();
            var newVehicleQuery = SystemAPI.QueryBuilder().WithAll<Vehicle>().WithNone<AvailableVehicle>().Build();

            var existingVehicleQuery = SystemAPI.QueryBuilder().WithAll<Vehicle, AvailableVehicle>().Build();
            var deletedVehicleQuery =
                SystemAPI.QueryBuilder().WithAll<AvailableVehicle>().WithNone<Vehicle>().Build();

            // update stable list of vehicles if they have changed
            if (newVehicleQuery.CalculateEntityCount() > 0 || deletedVehicleQuery.CalculateEntityCount() > 0)
            {
                state.EntityManager.AddComponent<AvailableVehicle>(newVehicleQuery);
                state.EntityManager.RemoveComponent<AvailableVehicle>(deletedVehicleQuery);

                allVehicles.Clear();
                using (var allVehicles = existingVehicleQuery.ToEntityArray(Allocator.TempJob))
                {
                    this.allVehicles.AddRange(allVehicles);
                }
            }

            // do nothing if there are no vehicles
            if (allVehicles.Length == 0)
            {
                return;
            }

            // validate active vehicle singleton
            var activeVehicle = Entity.Null;
            if (activeVehicleQuery.CalculateEntityCount() == 1)
            {
                activeVehicle = activeVehicleQuery.GetSingletonEntity();
            }
            else
            {
                using (var activeVehicles = activeVehicleQuery.ToEntityArray(Allocator.TempJob))
                {
                    Debug.LogWarning(
                        $"Expected exactly one {nameof(VehicleAuthoring)} component in the scene to be marked {nameof(VehicleAuthoring.ActiveAtStart)}. " +
                        "First available vehicle is being set to active."
                    );

                    // prefer the first vehicle prospectively marked as active
                    if (activeVehicles.Length > 0)
                    {
                        activeVehicle = activeVehicles[0];
                        state.EntityManager.RemoveComponent<ActiveVehicle>(allVehicles.AsArray());
                    }
                    // otherwise use the first vehicle found
                    else
                    {
                        activeVehicle = allVehicles[0];
                    }

                    state.EntityManager.AddComponent<ActiveVehicle>(activeVehicle);
                }
            }

            // do nothing else if there are no vehicles to change to or if there is no input to change vehicle
            if (allVehicles.Length < 2)
            {
                return;
            }

            var input = vehicleInputQuery.GetSingleton<VehicleInput>();
            if (input.Change == 0)
            {
                return;
            }

            // find the index of the currently active vehicle
            var activeVehicleIndex = 0;
            for (int i = 0, count = allVehicles.Length; i < count; ++i)
            {
                if (allVehicles[i] == activeVehicle)
                {
                    activeVehicleIndex = i;
                }
            }

            // if the active vehicle index has actually changed, then move the active vehicle tag to the new vehicle
            var numVehicles = allVehicles.Length;
            var newVehicleIndex = ((activeVehicleIndex + input.Change) % numVehicles + numVehicles) % numVehicles;
            if (newVehicleIndex == activeVehicleIndex)
            {
                return;
            }

            state.EntityManager.RemoveComponent<ActiveVehicle>(allVehicles.AsArray());
            state.EntityManager.AddComponent<ActiveVehicle>(allVehicles[newVehicleIndex]);
        }
    }

    struct ActiveVehicle : IComponentData
    {
    }
}
