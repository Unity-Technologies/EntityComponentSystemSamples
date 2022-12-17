using System;
using System.Collections.Generic;
using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using UnityEngine;

public struct UserWorldSingleton : IComponentData {}

public class UserWorldCreator : MonoBehaviour
{
    class UserWorldCreatorBaker : Baker<UserWorldCreator>
    {
        public override void Bake(UserWorldCreator authoring)
        {
            AddComponent<UserWorldSingleton>();
        }
    }
}

// We are checking here that the events from the non-default index worlds are:
// 1. Raised properly
// 2. Don't interfere with default world events (and vice versa)
// This is done to make sure that the simulation is saved and restored properly when using CustomPhysicsSystemGroup API
// Note: we are using trigger events only, as CollisionEvents don't get raised from deactivated bodies on Havok, so it would be impossible to test.
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial class UserPhysicsGroup : CustomPhysicsSystemGroup
{
    public UserPhysicsGroup() : base(1, true) {}

    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate<UserWorldSingleton>();
    }

    protected override void AddExistingSystemsToUpdate(List<Type> systems)
    {
        systems.Add(typeof(TriggerEventsCountSystem_InPhysicsGroup));
    }

    protected override void PreGroupUpdateCallback()
    {
        base.PreGroupUpdateCallback();

        // Disable data clean in the group which updates after main physics.
        World.GetExistingSystemManaged<CleanPhysicsDebugDataSystem>().Enabled = false;
    }

    protected override void PostGroupUpdateCallback()
    {
        base.PostGroupUpdateCallback();

        // Restore data clean for the main physics - since it updates after main physics.
        World.GetExistingSystemManaged<CleanPhysicsDebugDataSystem>().Enabled = true;
    }
}

// An enum representing update orders of systems that we are trying to check in this scene. Used for debug purposes in cases something goes wrong.
public enum SystemUpdateOrderEnum
{
    BeforePhysicsGroup,
    InPhysicsGroup,
    AfterPhysicsGroup
}

public partial struct CheckEventCountJob : IJob
{
    public NativeReference<int> EventCount;

    public int WorldIndex;
    public SystemUpdateOrderEnum UpdateOrder;

    public void Execute()
    {
        // There are zero events until the body falls on ground.
        // 6 and 12 represent number of dynamic bodies per world in the scene, and we expect one event per body.
        if (WorldIndex == 0)
        {
            Assert.IsTrue(EventCount.Value == 6 || EventCount.Value == 0, $"In {UpdateOrder} the event count is not matching! Expected 0 or 6, but got {EventCount.Value}. World Index 0.");
        }

        if (WorldIndex == 1)
        {
            Assert.IsTrue(EventCount.Value == 12 || EventCount.Value == 0, $"In {UpdateOrder} the event count is not matching! Expected 0 or 12, but got {EventCount.Value}. World Index 1.");
        }
    }
}

public partial struct CountTriggerEventsJob : ITriggerEventsJob
{
    public NativeReference<int> EventCount;

    public void Execute(TriggerEvent triggerEvent)
    {
        EventCount.Value++;
    }
}

#region Trigger event systems

[UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
public partial class TriggerEventsCountSystem_BeforePhysicsGroup : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate<UserWorldSingleton>();
    }

    protected override void OnUpdate()
    {
        NativeReference<int> eventCount = new NativeReference<int>(0, Allocator.TempJob);

        Dependency = new CountTriggerEventsJob { EventCount = eventCount }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);
        Dependency = new CheckEventCountJob { EventCount = eventCount, UpdateOrder = SystemUpdateOrderEnum.BeforePhysicsGroup, WorldIndex = (int)SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorldIndex.Value }.Schedule(Dependency);
        Dependency = eventCount.Dispose(Dependency);
    }
}

[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateAfter(typeof(PhysicsSimulationGroup))]
[UpdateBefore(typeof(ExportPhysicsWorld))]
public partial class TriggerEventsCountSystem_InPhysicsGroup : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate<UserWorldSingleton>();
    }

    protected override void OnUpdate()
    {
        NativeReference<int> eventCount = new NativeReference<int>(0, Allocator.TempJob);

        Dependency = new CountTriggerEventsJob { EventCount = eventCount }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);
        Dependency = new CheckEventCountJob { EventCount = eventCount, UpdateOrder = SystemUpdateOrderEnum.InPhysicsGroup, WorldIndex = (int)SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorldIndex.Value }.Schedule(Dependency);
        Dependency = eventCount.Dispose(Dependency);
    }
}

[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
public partial class TriggerEventsCountSystem_AfterPhysicsGroup : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate<UserWorldSingleton>();
    }

    protected override void OnUpdate()
    {
        NativeReference<int> eventCount = new NativeReference<int>(0, Allocator.TempJob);

        Dependency = new CountTriggerEventsJob { EventCount = eventCount }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), Dependency);
        Dependency = new CheckEventCountJob { EventCount = eventCount, UpdateOrder = SystemUpdateOrderEnum.AfterPhysicsGroup, WorldIndex = (int)SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorldIndex.Value }.Schedule(Dependency);
        Dependency = eventCount.Dispose(Dependency);
    }
}

#endregion
