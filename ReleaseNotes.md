# 0.0.18
## New Features

## Upgrade guide

## Changes
* Restructured documentation and revised a lot of content. New reference page contains an index of topics. Some pages still contain stubs to be filled in, and other pages moved to under_review section if they will be subject to further, more drastic revisions.

## Fixes
* Fixed a race condition in NativeQueue causing memory corruption leading to editor crashes


# 0.0.17
## New Features
* Entity Debugger now has an option to show chunk info for any given query. Click "Chunk Info" in the upper right to see chunk usage data for each archetype.

## Upgrade guide

## Changes
* Updated burst to 0.2.4-preview.33

## Fixes
* Fixed bug when instantiating prototype with DynamicBuffer where data would be written out of bounds and could cause a crash.
* Fixed NotSupportedException when DefaultWorldInitialization fails to load a type from a dynamic assembly.
* Fixed an issue where EntityDebugger caused a stack overflow when determining the name of types nested in generic types

# 0.0.16
## New Features
* Added virtual `ValidateSerializedData()` method to `ComponentDataWrapper<T>`and `SharedComponentDataWrapper<T>`, which allows you to sanitize the wrapper's serialized data.

## Upgrade guide

## Changes
* Reverted hotfix in 0.0.14 that made `ComponentDataWrapperBase.OnValidate()` public and `ComponentDataWrapper<T>.m_SerializedData` protected; both are private again.
* CopyTransformToGameObjectSystem and CopyTransformFromGameObjectSystem now execute in edit mode.

## Fixes
* Fixed selection not working in Galactic Conquest sample.
* Fixed errors in HierarchyBrokenExample, HierarchyExample, and RotationExample.
* Fixed regression introduced in 0.0.14 that caused typing values for a RotationComponent in the Inspector to re-normalize with every (xyzw) component entry.
* Fixed all warnings in samples and packages.
* Fixed bug that prevented entering Prefab isolation mode while in play mode in 2018.3, if the Prefab contained BaseComponentDataWrapper components.

# 0.0.15
## New Features

## Upgrade guide

## Changes
* By default, EntityDebugger doesn't show inactive systems (systems which have never run). You can choose to show them in the World dropdown.
* Fixed an issue where closing the EntityDebugger's Filter window would throw an exception
* The Unity.Entities assembly no longer references the UnityEngine.Component type directly. If you create a build that strips the Unity.Entities.Hybrid assembly, but you need to create a ComponentType instance from a UnityEngine.Component-derived type, you must first manually call `TypeManager.RegisterUnityEngineComponentType(typeof(UnityEngine.Component))` somewhere in your initialization code.
* EntityCommandBuffer now records which system the commandbuffer was recorded in and which barrier it is played back and it includes it when an exception is thrown on playback of the command buffer.

## Fixes
* Fixed memory corruption where EntityCommandBuffer.AddComponent with zero sized components overwriting memory of other components. (This is a regression that was introduced in 0.0.13_


# 0.0.14
## Fixes
* Fixed a bug which was causing some of the samples to not work correctly


# 0.0.13
## New Features
* Added additional warnings to the Inspector for ComponentDataWrapper and SharedComponentDataWrapper types related to multiple instances of the same wrapper type.

## Upgrade guide
* All ComponentDataWrapper types shipped in this package are now marked with `DisallowMultipleComponent` in order to prevent unexpected behavior, since an Entity may only have a single component of a given type. If you have any GameObjects with multiples of a given ComponentDataWrapper type, you must remove the duplicates. (Due to an implentation detail in the current hybrid serialization utility, SharedComponentDataWrapper types cannot be marked as such. This issue will be addressed in a future release.)

## Changes
* ComponentDataWrapperBase now implements `protected virtual OnEnable()` and `protected virtual OnDisable()`. You must override these methods and call the base implementation if you had defined them in a subclass.
* GameObjectEntity `OnEnable()` and `OnDisable()` are now `protected virtual`, instead of `public`.

## Fixes
* Fixed bug where component data was not immediately registered with EntityManager when adding a ComponentDataWrapper to a GameObject whose GameObjectEntity had already been enabled.
* Fixed a bug where EntityManager.AddComponentData would throw an exception when adding a zero sized / tag component.
* Fixed hard crash in `SerializeUtilityHybrid.SerializeSharedComponents()` when the SharedComponentDataWrapper for the SharedComponentData type was marked with `DisallowMultipleComponent`. It now throws an exception instead.

# 0.0.12
## New Features

## Upgrade guide
* OnCreateManager(int capacity) -> OnCreateManager(). All your own systems have to be changed to follow the new signature.

## Changes
* Removed capacity parameter from from ScriptBehaviourManager.OnCreateManager.
* EntityDebugger now displays the declaring type for nested types
* IncrementalCompiler is no longer a dependency on the entities package. If you want to continue to use it you need to manually include it from the package manager UI for your project.
* `EntityCommandBuffer.Concurrent` playback is now deterministic. Playback order is determined by the new `jobIndex` parameter accepted by all public API methods, which must be a unique ID per job (such as the index passed to `Execute()` in an IJobParallelFor).

## Fixes
* Fixed bug where ComponentDataWrapper fields spilled out of their area in the Inspector.
* ComponentDataWrapper for empty data types (i.e. tags) no longer displays error in Inspector if wrapped type is not serializable.
* Fixed an issue where EntityDebugger was slow if you scrolled down past 3 million entities

# 0.0.11
## New Features
* Global `Disabled` component. Any component data associated with same entity referenced by `Disabled` component will be ignored by all system updates.
* Global `Prefab` component. Same behavior as `Disabled` component, except when an Entity associated with a `Prefab` component is Instantiated, the `Prefab` component is not present in the created archetype.
* EntityCommandBuffer.Instantiate API has been added
* Added custom editor for `ComponentDataWrapper<T>` and `SharedComponentDataWrapper<T>`, which will display an error in the Inspector if the encapsulated data type is not marked as serializable
* new IJobProcessComponentDataWithEntity job type extends IJobProcessComponentData and passes Entity & int foreachIndex. This makes it possible to use it in jobs using EntityCommandBuffer.
* BufferDataFromEntity renamed to BufferFromEntity. ComponentSystem.GetBufferArrayFromEntity has been renamed to ComponentSystem.GetBufferFromEntity.

## Changes
* Serialized component data for `ComponentDataWrapper<T>` and `SharedComponentDataWrapper<T>` classes now appears in the Inspector without a foldout group
* IJobProcessComponentData supports up to 4 components now.
* IJobProcessComponentData.Schedule function no longers takes the number of batch iteration count. Batch iteration count is now always implicit to be the size of a whole chunk. This requires changing all code using IJobProcessComponentData.
* IJobProcessComponentData.ScheduleSingle can be used to execute IJobProcessComponentData in a single job. IJobProcessComponentData.Schedule on the other hand by default schedules parallel for jobs.
* ForEachComponentGroupFilter has been removed. We recommend ArchetypeChunk API as a replacement (Documentation/content/chunk_iteration.md)
* `TransformSystem` is now an abstract class and no longer have a generic `<T>` parameter
* Removed MeshCulledComponent & MeshCullingComponent. They were accidentally still left after the rewrite of the InstanceRendererSystem in preview 11.

## Fixes
* Fixed bug where `Value` setter on `ComponentDataWrapper<T>` or `SharedComponentDataWrapper<T>` did not push changes back to `EntityManager` (fixes the inability to flush changes via `Value` setter + `Undo.RecordObject()` while Inspector was drawing)
* Removed sync point in GetComponentGroup resulting in two IJobProcessComponentData in the same system to fail on first exectuion.
* Added more robust checks for what defines a valid IComponentData (must be blittable / must be a struct etc)
* Fixed a bug with `TransformSystem` jobs (e.g `RootLocalToWorld`) not being compiled by burst for standalone players

# 0.0.10
## New Features
* [Dynamic Buffers](Documentation/content/dynamic_buffers.md) (FixedArray functionality has been removed.)
* [Chunk Iteration](Documentation/content/chunk_iteration.md)
* [TransformSystem](Documentation/content/transform_system.md) 
  * Note: Completely incompatible with previous version.
  * Some Components "downgraded" to Samples.Common (not part of Unity.Transforms) - MoveForward, MoveSpeed, Heading, RotationSpeed
  * Transform2D Removed.
* [SystemStateComponents](Documentation/content/system_state_components.md)
* EntityCommandBuffer.Concurrent added to support command buffer recording in parallel for-type jobs
* EntityManager.MoveEntitiesFrom optimizations (Moving real world scene with 50k entities takes less than 1ms now)
* Unity.Entities.Serialization API for writing binary scene format (No backwards compatibility, but incredibly fast load speed)

## Changes
* **Unity 2018.1 is no longer supported. The Entities package now requires a minimum version of 2018.2f1**
* EntityDebugger is now much faster when structural changes affect the list of entities being viewed.
* Moved EntityDebugger to Window/Analysis submenu
* EntityDebugger shows EntityArchetypeQuery fields in addition to ComponentGroups, in order to show useful contents for systems that use chunk iteration.
* EntityDebugger shows systems in the order they appear in the player loop

# 0.0.9
## New Features
* Galactic Conquest sample added
    - Left click to select planets of the same color
    - Right click to send ships from the selected planets to the planet under the mouse
    - Can be set to play by itself if running the SceneSwitcher scene
* GravityDemo sample added
    - Press 1-7 on the keyboard while it's running to change to different simulations
    - Left click to spawn new asteroids from the camera
    - While holding right click, move the mouse and use AWSD buttons to control the camera

# 0.0.8
## New Features

* EntityCommandBuffer.Concurrent added to support command buffer recording in parallel for-type jobs

## Changes

* Fixed the check for blittable types in NativeHashMap and NativeMultiHashMap values
* Change deprecated attribute `[ComputeJobOptimization]` to `[BurstCompile]` (from namespace `Unity.Burst`)
* Fixed bug with entity batch deletes (#149)

# 0.0.7
## New Features
* New system for frustum culling meshes processed by MeshInstanceRendererSystem. Add a MeshCullingComponent to the entity and it will only be rendered when it is in view. The culling system does not take shadows into account.
* New system for LOD of meshes rendered with MeshInstanceRendererSystem.
    - MeshLODGroupComponent defines the lod sizes and active lod.
	- MeshLODComponent references an Entity with a MeshLODGroupComponent and enables / disables itself based on the specified active lod. Transforms between mesh and group must match.
* Entity worlds can now be serialized and deserialized to/from a binary format using SerializeUtility
    - Use SerializeUtilityHybrid to support shared components
		
## Changes
* EntityDebugger's display of ComponentGroups is improved:
    - They will now wrap to multiple lines if there isn't enough space
    - Generic types are displayed nicely
    - Sort order is stable

# 0.0.6
## New Features
* OnStartRunning() and OnStopRunning() added to ComponentSystem and JobComponentSystem
    - OnStartRunning is called when a system's Enabled or ShouldStartRunning() becomes true
    - OnStopRunning is called when a system's Enabled or ShouldStartRunning() becomes false. Also when the system will get destroyed.
    - It will only send one of each in succession
        - Example: Two OnStartRunning() cannot be triggered for a given system without an OnStopRunning() call in between
* Experimental SOA containers updated, now split into two different types:
    - NativeArrayFullSOA internally lays everything out in sub-arrays
    - NativeArrayChunked8 internally lays data out in chunks of 32 bytes
* Component type versions in Chunks (for broadphase change tracking)
* Query Archetype and Chunk iteration (query archetypes matching all/any/none component filter, and e.g. allow component existence checks on chunk level.)
* Add SystemStateComponentData (answer to Reactive system for add/delete components)
* IComponentSystemPatch to auto run ComponentSystem[Job] after every ComponentSystem.

## Changes
* Make it possible to create EntityArray in addition to ComponentDataArray with the new ForEachFilter

# 0.0.5
## New Features
* New API for faster filtering when going through all unique shared component values.
	- var filter = group.CreateForEachFilter(uniqueTypes);
	- var array = group.GetComponentDataArray<Type>(filter, i); // in a loop
	- filter.Dispose();

## Changes
* Throw ArgumentException when creating an entity with component data exceeding chunk size (64kb)
* EntityManager.CreateComponentGroup is no longer public, use ComponentSystem.GetComponentGroup instead
* Fix an incorrect hash calculation when resizing a HashMap

# 0.0.4

## New Features
* New Entity Debugger replaces EntityWindow and SystemWindow
	- Lists Systems, allowing you to browse the Entities in each of their ComponentGroups
	- Systems that are not running due to empty ComponentGroups will appear greyed out
	- Systems can be enabled and disabled temporarily for testing purposes
	- System main thread time is shown. Job time is not currently exposed (the Profiler is a more robust tool for this)
	- Selecting an Entity will show it in the inspector. This support is rudimentary, but will improve soon.

## Changes
* ComponentGroup.GetVariant replaced by ComponentGroup.SetFilter. The ComponentGroup is reused and simply chnages the filter on this ComponentGroup. 
	- Reduces GC allocations, since only one ComponentGroup will ever be created.
	- Fixes bug where shared component data indices would go out of sync when used on a job.
* EntityArray used in jobs must be marked [ReadOnly] now.


# 0.0.3

## Changes

* An `EntityCommandBuffer` that plays back automically after a `ComponentSystem`'s update is
  available as `PostUpdateCommands`

* Can now create entities/components from jobs and merge them into
  the world later via command buffers from injected `BarrierSystem`s
* `DeferredEntityChangeSystem` replaced by `EndFrameBarrier` (Note: This removes support for concurrent add/remove components. You'll need to change to IJob to add/remove components.)
  
* `NativeArraySharedValues<T>` for creating index tables of shared/unique values in a NativeArray.
* `NearestTargetPositionSystem<TNearestTarget,TTarget>` demonstrates how to use generics in JobComponentSystem
* `CopyComponentData<TSource,TDestination>` utility to copy ISingleValue ComponentData to NativeArray


* UnityPackageManager -> Packages folder. (Unity 2018.1 beta 7 introduces this change and we reflected it in the sample project)

* EntityManager.CreateComponentGroup should be replaced with ComponentSystem.GetComponentGroup.
It automatically associates & caches the ComponentGroup with the system (It is automatically disposed by ComponentSystem) and thus input dependencies will be setup correctly. Additionally ComponentSystem.GetComponentGroup should not be called in OnUpdate() (It is recommended to create and cache in OnCreateManager instead). ComponentSystem.GetComponentGroup allocates GC memory because the input is a param ComponentType[]...

* Systems are automatically disabled when all ComponentGroups have zero entities.
[AlwaysUpdateSystem] can be used to always force update a system.
(We measured 5 - 10x speedup for empty systems)

* EntityManager.GetComponentFromEntity/GetFixedArrayFromEntity have been moved to JobComponentSystem.GetComponentFromEntity. This way they can be safely used in jobs with the correct dependencies passed via the OnUpdate (JobHandle dependency)

* EntityManager.GetComponentFromEntity/GetFixedArrayFromEntity have been moved to JobComponentSystem.GetComponentFromEntity. This way they can be safely used in jobs with the correct dependencies passed via the OnUpdate (JobHandle dependency)

* Removed IAutoComponentSystemJob support

* Various namespace refactoring. Unity.ECS -> Unity.Entities.

* Optimizations for NativeHashMap and NativeMultiHashMap

* Can now get an array of shared component data from a component group (ComponentGroup.GetSharedComponentDataArray)
  SharedComponentDataArray<T> can also be injected similar to ComponentDataArray<T>
  Access through SharedComponentDataArray is always read only

* IJobProcessComponentData is significantly simplified. Supports 1, 2, 3 parameters. Supports read only, supports additional required components & subtractive components. https://github.com/Unity-Technologies/ECSJobDemos/blob/stable/ECSJobDemos/Assets/GameCode/SimpleRotation/RotationSpeedSystem.cs

# 0.0.2

## New Features

## Changes
* [InjectComponentGroup] and [InjectComponentFromEntity] were replaced by simply [Inject] handling all injection cases.
* EntityManager component naming consistency renaming
	EntityManager can access both components and component data thus:
	- HasComponent(ComponentType type), RemoveComponent(ComponentType type), AddComponent(ComponentType type)
	- AddComponentData(Entity entity, T componentData) where T : struct, IComponentData

	* EntityManager.RemoveComponentData -> EntityManager.RemoveComponent
	* EntityManager.AddComponent(...) : IComponentData -> EntityManager.AddComponentData(...) : IComponentData
	* EntityManager.AddSharedComponent -> EntityManager.AddSharedComponentData
	* EntityManager.SetSharedComponent -> EntityManager.SetSharedComponentData
	* EntityManager.SetComponent -> EntityManager.SetComponentData
	* EntityManager.GetAllUniqueSharedComponents -> EntityManager.GetAllUniqueSharedComponentDatas


## Fixes

# 0.0.1

## New Features
* Burst Compiler Preview
    * Used to compile an C# jobs, simply put  [ComputeJobOptimization] on each job 
    * Editor only for now, it is primarily meant to give you an idea of the performance you can expect when we ship the full AOT burst compiler
    * Compiles asynchronously, once compilation of the job completes. The runtime switches to using the burst compiled code.
* EntityTransaction API added to allow for creating entities from a job

## Improvements
* NativeQueue is now block based and always have a dynamic capacity which cannot be manually set or queried.
* Worlds have names and there is now a full list of them
* SharedComponentData API is now robust, performs automatic ref counting, and no longer leaks memory. SharedComponent API redesigned.
* Optimization for iterating component data arrays and EntityFromComponentData
* EntityManager.Instantiate, EntityManager.Destroy, CreateEntity optimizations


## Fixes
Fix a deadlock in system order update
