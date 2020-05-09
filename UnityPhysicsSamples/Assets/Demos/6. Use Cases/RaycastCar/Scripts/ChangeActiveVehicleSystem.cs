using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

struct ActiveVehicle : IComponentData { }

[UpdateAfter(typeof(DemoInputGatheringSystem))]
class ChangeActiveVehicleSystem : ComponentSystem
{
    struct AvailableVehicle : ISystemStateComponentData { }

    EntityQuery m_ActiveVehicleQuery;
    EntityQuery m_VehicleInputQuery;

    EntityQuery m_NewVehicleQuery;
    EntityQuery m_ExistingVehicleQuery;
    EntityQuery m_DeletedVehicleQuery;

    NativeList<Entity> m_AllVehicles;

    protected override void OnCreate()
    {
        m_ActiveVehicleQuery = GetEntityQuery(typeof(ActiveVehicle), typeof(Vehicle));
        m_VehicleInputQuery = GetEntityQuery(typeof(VehicleInput));
        m_NewVehicleQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<Vehicle>() },
            None = new[] { ComponentType.ReadOnly<AvailableVehicle>() }
        });
        m_ExistingVehicleQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<Vehicle>(), ComponentType.ReadOnly<AvailableVehicle>() }
        });
        m_DeletedVehicleQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<AvailableVehicle>() },
            None = new[] { ComponentType.ReadOnly<Vehicle>() }
        });

        m_AllVehicles = new NativeList<Entity>(Allocator.Persistent);
    }

    protected override void OnDestroy() => m_AllVehicles.Dispose();

    protected override void OnUpdate()
    {
        // update stable list of vehicles if they have changed
        if (m_NewVehicleQuery.CalculateEntityCount() > 0 || m_DeletedVehicleQuery.CalculateEntityCount() > 0)
        {
            EntityManager.AddComponent(m_NewVehicleQuery, typeof(AvailableVehicle));
            EntityManager.RemoveComponent<AvailableVehicle>(m_DeletedVehicleQuery);

            m_AllVehicles.Clear();
            using (var allVehicles = m_ExistingVehicleQuery.ToEntityArray(Allocator.TempJob))
                m_AllVehicles.AddRange(allVehicles);
        }

        // do nothing if there are no vehicles
        if (m_AllVehicles.Length == 0)
            return;

        // validate active vehicle singleton
        var activeVehicle = Entity.Null;
        if (m_ActiveVehicleQuery.CalculateEntityCount() == 1)
            activeVehicle = m_ActiveVehicleQuery.GetSingletonEntity();
        else
        {
            using (var activeVehicles = m_ActiveVehicleQuery.ToEntityArray(Allocator.TempJob))
            {
                Debug.LogWarning(
                    $"Expected exactly one {nameof(VehicleAuthoring)} component in the scene to be marked {nameof(VehicleAuthoring.ActiveAtStart)}. " +
                    "First available vehicle is being set to active."
                );

                // prefer the first vehicle prospectively marked as active
                if (activeVehicles.Length > 0)
                {
                    activeVehicle = activeVehicles[0];
                    EntityManager.RemoveComponent<ActiveVehicle>(m_AllVehicles);
                }

                // otherwise use the first vehicle found
                else
                    activeVehicle = m_AllVehicles[0];

                EntityManager.AddComponent(activeVehicle, typeof(ActiveVehicle));
            }
        }

        // do nothing else if there are no vehicles to change to or if there is no input to change vehicle
        if (m_AllVehicles.Length < 2)
            return;

        var input = m_VehicleInputQuery.GetSingleton<VehicleInput>();

        if (input.Change == 0)
            return;

        // find the index of the currently active vehicle
        var activeVehicleIndex = 0;
        for (int i = 0, count = m_AllVehicles.Length; i < count; ++i)
        {
            if (m_AllVehicles[i] == activeVehicle)
                activeVehicleIndex = i;
        }

        // if the active vehicle index has actually changed, then move the active vehicle tag to the new vehicle
        var numVehicles = m_AllVehicles.Length;
        var newVehicleIndex = ((activeVehicleIndex + input.Change) % numVehicles + numVehicles) % numVehicles;
        if (newVehicleIndex == activeVehicleIndex)
            return;

        EntityManager.RemoveComponent<ActiveVehicle>(m_AllVehicles);
        EntityManager.AddComponent(m_AllVehicles[newVehicleIndex], typeof(ActiveVehicle));
    }
}
