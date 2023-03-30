# Systems

A **system** is a unit of code which belongs to a [world]() and which runs on the main thread (usually once per frame). Normally, a system will only access entities of its own world, but this is not an enforced restriction.

&#x1F579; *[See example systems](./examples/components_systems.md#system-and-systemgroup).*

A system is defined as a struct implementing the [`ISystem`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.ISystem.html) interface, which has three key methods:

| **`ISystemState` method** | **Description** |
|---|---|
| [`OnUpdate()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.ISystem.OnUpdate.html) | Normally called once per frame, though this depends upon the `SystemGroup` to which the system belongs. |
| [`OnCreate()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.ISystem.OnCreate.html) | Called before the first call to `OnUpdate` and whenever a system resumes running. |
| [`OnDestroy()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.ISystem.OnDestroy.html) | Called when a system is destroyed. |

A system may additionally implement `ISystemStartStop`, which has these methods:

| **`ISystemStartStop` method** | **Description** |
|------|------|
| [`OnStartRunning()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.ISystemStartStop.OnStartRunning.html) | Called before the first call to `OnUpdate` and after any time the system's [`Enabled`](xref:Unity.Entities.ComponentSystemBase.Enabled) property is changed from `false` to `true`. |
| [`OnStopRunning()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.ISystemStartStop.OnStopRunning.html) | Called before `OnDestroy` and after any time the system's [`Enabled`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.SystemState.Enabled.html) property is changed from `true` to `false`. |

<br>

## System groups and system update order

The systems of a world are organized into **system groups**. Each system group has an ordered list of systems and other system groups as its children, so the system groups form a hierarchy, which determines the update order. A system group is defined as a class inheriting from [`ComponentSystemGroup`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.ComponentSystemGroup.html).

&#x1F579; *[See example system groups](./examples/components_systems.md#system-and-systemgroup).*

When a system group is updated, the group normally updates its children in their sorted order, but this default behavior can be overridden by overriding the group's update method.

A group's children are re-sorted every time a child is added or removed from the group.

The [`[UpdateBefore]`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.UpdateBeforeAttribute.html) and [`[UpdateAfter]`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.UpdateAfterAttribute.html) attributes can be used to determine the relative sort order amongst the children in a group. For example, if a *FooSystem* has the attribute `UpdateBefore(typeof(BarSystem))]`, then *FooSystem* will be put somewhere before *BarSystem* in the sorted order. If, however, *FooSystem* and *BarSystem* don't belong to the same group, the attribute is ignored. If the ordering attributes of a group's children create a contradiction (*e.g.* *A* is marked to update before *B* but also *B* is marked to update before *A*), an exception is thrown when the group's children are sorted.

<br>

## Creating worlds and systems

By default, an automatic bootstrapping process creates a default world with three system groups: 

- `InitializationSystemGroup`, which updates at the end of the `Initialization` phase of the Unity player loop.
- `SimulationSystemGroup`, which updates at the end of the `Update` phase of the Unity player loop.
- `PresentationSystemGroup`, which updates at the end of the `PreLateUpdate` phase of the Unity player loop.

Automatic bootstrapping creates an instance of every system and system group (except those with the [`[DisableAutoCreation]`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.DisableAutoCreationAttribute.html) attribute). These instances are added added to the `SimulationSystemGroup` unless overridden by the [`[UpdateInGroup]`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.UpdateInGroupAttribute.html) attribute. For example, if a system has the attribute `UpdateInGroup(typeof(InitializationSystemGroup))]`, then the system will be added to the `InitializationSystemGroup` instead of the `SimulationSystemGroup`.

The automatic bootstrapping process can be disabled with scripting defines:

|**Scripting define**|**Description**|
|---|---|
|`#UNITY_DISABLE_AUTOMATIC_SYSTEM_BOOTSTRAP_RUNTIME_WORLD`| Disables automatic bootstrapping of the default world. |
|`#UNITY_DISABLE_AUTOMATIC_SYSTEM_BOOTSTRAP_EDITOR_WORLD`| Disables automatic bootstrapping of the Editor world. |
|`#UNITY_DISABLE_AUTOMATIC_SYSTEM_BOOTSTRAP`| Disables automatic bootstrapping of both the default world and the Editor world. |

When automatic bootstrapping is disabled, your code is responsible for:

- Creating any worlds you need.
- Calling [`World.GetOrCreateSystem<T>()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.World.GetOrCreateSystem.html) to add the system and system group instances to the worlds.
- Registering top-level system groups (like `SimulationSystemGroup`) to update in the Unity [PlayerLoop](https://docs.unity3d.com/ScriptReference/LowLevel.PlayerLoop.html).

Alternatively, automatic bootstrapping can be customized by creating a class that implements [`ICustomBootstrap`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.ICustomBootstrap.html).

&#x1F579; *[See examples of world creation and customized bootstrapping](./examples/bootstrapping.md).*

<br>

## Time in worlds and systems

A world has a `Time` property, which returns a [`TimeData`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Core.TimeData.html) struct, containing the frame delta time and elapsed time. The time value is updated by the world's [`UpdateWorldTimeSystem`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.UpdateWorldTimeSystem.html). The time value can be manipulated with these `World` methods:

|**`World` method**|**Description**|
|---|---|
| [`SetTime`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.World.SetTime.html) | Set the time value. |
| [`PushTime`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.World.PushTime.html) | Temporarily change the time value. |
| [`PopTime`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.World.PopTime.html) | Restore the time value from before the last push. |

Some system groups, like [`FixedStepSimulationSystemGroup`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.FixedStepSimulationSystemGroup.html), push a time value before updating their children and then pop the value once done updating.

<br>

## SystemState

A system's `OnUpdate()`, `OnCreate()`, and `OnDestroy()` methods are passed a [`SystemState`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.SystemState.html) parameter. `SystemState` represents the state of the system instance and has important methods and properties, including:

|**Method or property**|**Description**|
|---|---|
| [`World`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.SystemState.World.html) | The system's world. |
| [`EntityManager`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.SystemState.EntityManager.html) | The `EntityManager` of the system's world. |
| [`Dependency`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.SystemState.Dependency.html) | A `JobHandle` used to pass job dependencies between systems. |
| [`GetEntityQuery()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.SystemState.GetEntityQuery.html) | Returns an `EntityQuery`. |
| [`GetComponentTypeHandle<T>()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.SystemState.GetComponentTypeHandle.html) | Returns a `ComponentTypeHandle<T>`. |
| [`GetComponentLookup<T>()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.SystemState.GetComponentLookup.html) | Returns a `ComponentLookup<T>`.|

| &#x26A0; IMPORTANT |
| :- |
| Although entity queries, component type handles, and component lookups can be acquired directly from the `EntityManager`, it is generally proper for a system to only acquire these things from the `SystemState` instead. By going through `SystemState`, the component types accessed get tracked by the system, which is essential for the `Dependency` property to correctly pass job dependencies between systems. *[See more about jobs that access entities](./entities-jobs.md).* |

<br>

## SystemAPI

The [`SystemAPI`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.SystemAPI.html) class has many static convenience methods, covering much of the same functionality as `World`, `EntityManager`, and `SystemState`.

The `SystemAPI` methods rely upon [source generators](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview), so they only work in systems and `IJobEntity` (but not `IJobChunk`). The advantage of using `SystemAPI` is that these methods produce the same results in both contexts, so code that uses `SystemAPI` will generally be easier to copy-paste between these two contexts.

| &#x1F4DD; NOTE |
| :- |
| If you get confused about where to look for key Entities functionality, the general rule is to check `SystemAPI` first. If `SystemAPI` doesn't have what you're looking for, look in `SystemState`, and if what you're looking for isn't there, look in the `EntityManager` and `World`. |

`SystemAPI` also provides a special [`Query()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.SystemAPI.Query.html) method that, through source generation, helps conveniently create a foreach loop over the entities and components matching a query.

&#x1F579; *[See examples of using `SystemAPI.Query()`](./examples/components_systems.md#querying-for-entities).*

<br>