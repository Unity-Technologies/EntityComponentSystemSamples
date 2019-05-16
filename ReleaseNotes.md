# Samples 29

To view the changelog for a the package, go to **Package Manager** and click on **View changelog** or open the respective `CHANGELOG.md` file inside the package folder. 

## Changes

* Rearranged all HelloECS samples into new HelloCube folder structure. Also renamed most types to make it clear in the Unity UI exactly what types are being used (namespaces are not typically visible in Unity).
* Moved the `Hybrid_01_FixedTimestep` to `Advanced/FixedTimestepWorkaround`.


# 0.0.28

Please note that the version of the Samples (`0.0.28`) is not related to the preview version of entities (`preview.31`).
To view the changelog for a the package, go to **Package Manager** and click on **View changelog**. 

## New Samples

* Added new sample, HelloCube_08_SpawnAndRemove, to demonstrate both creating and removing entities at runtime.
* Added a hybrid sample of driving a component system with a fixed timestep. See `Samples/Assets/HelloECS/Hybrid_01_FixedTimestep`. The method demonstrated in this sample is intended as a short-term workaround; the entire `SimulationSystemGroup` will eventually use a fixed timestep by default.

* Added versions of `IJobForEach` that support `DynamicBuffer`s
  * Due to C# language contraints, these overloads needed different names. For example:
  * `IJobForEach_BCC` is a job which takes 1 `IBufferElementData` and 2 `IComponentData`
  * `IJobForEach_BBC` is a job which takes 2 `IBufferElementData` and 1 `IComponentData`
  * ...etc

## Upgrade guide

* Serialized entities file format version has changed, Sub Scenes entity caches will require rebuilding.

## Changes

* Rebuilding the entity cache files for sub scenes will now properly request checkout from source control if required.

## Fixes

* `IJobForEach` will only create new entity queries when scheduled, and won't rely on injection anymore. This avoids the creation of useless queries when explicit ones are used to schedule those jobs. Those useless queries could cause systems to keep updating even though the actual queries were empty.
* LODGroup conversion now handles renderers being present in a LOD Group in multipe LOD levels correctly
* Fixed an issue where chunk utilization histograms weren't properly clipped in EntityDebugger
* Fixed an issue where tag components were incorrectly shown as subtractive in EntityDebugger

## Known issues

# 0.0.27

## New Features

* Script templates have been added to help you create new component types and systems, similar to Unity's built-in template for new MonoBehaviours. Use them via the `Assets/Create/ECS` menu.

## Upgrade guide

### Some APIs have been deprecated in this release

* [API Deprecation FAQ](https://forum.unity.com/threads/api-deprecation-faq-0-0-23.636994/)
** Removed obsolete `ComponentSystem.ForEach`
** Removed obsolete `[Inject]`
** Removed obsolete `ComponentDataArray`
** Removed obsolete `SharedComponentDataArray`
** Removed obsolete `BufferArray`
** Removed obsolete `EntityArray`
** Removed obsolete `ComponentGroupArray`

### ScriptBehaviourManager removal

* The `ScriptBehaviourManager` class has been removed.
* `ComponentSystem` and `JobComponentSystem` remain as system base classes (with a common `ComponentSystemBase` class)
** ComponentSystems have overridable methods `OnCreateManager` and `OnDestroyManager`.  These have been renamed to `OnCreate` and `OnDestroy`.
*** This is NOT handled by the obsolete API updater and will need to be done manually.
*** The old OnCreateManager/OnDestroyManager will continue to work temporarily, but will print a warning if a system contains them.
* World APIs have been updated as follows:
** `CreateManager`, `GetOrCreateManager`, `GetExistingManager`, `DestroyManager`, `BehaviourManagers` have been renamed to `CreateSystem`, `GetOrCreateSystem`, `GetExistingSystem`, `DestroySystem`, `Systems`.
*** These should be handled by the obsolete API updater.
** `EntityManager` is no longer accessed via `GetExistingManager`.  There is now a property directly on World: `World.EntityManager`.
*** This is NOT handled by the obsolete API updater and will need to be done manually.
*** Searching and replacing `Manager<EntityManager>` should locate the right spots.  For example, `world.GetExistingManager<EntityManager>()` should become just `world.EntityManager`.

### IJobProcessComponentData renamed to IJobForeach

* This rename unfortunately cannot be handled by the obsolete API updater.
* A global search and replace of `IJobProcessComponentData` to `IJobForEach` should be sufficient.

### ComponentGroup renamed to EntityQuery

* `ComponentGroup` has been renamed to `EntityQuery` to better represent what it does.
* All APIs that refer to `ComponentGroup` have been changed to refer to `EntityQuery` in their name, e.g. `CreateEntityQuery`, `GetEntityQuery`, etc.

### EntityArchetypeQuery renamed to EntityQueryDesc

* `EntityArchetypeQuery` has been renamed to `EntityQueryDesc`

## Changes

* Minimum required Unity version is now 2019.1.0b9
* Adding components to entities that already have them is now properly ignored.
* UNITY_CSHARP_TINY is now NET_DOTS to match our other NET_* defines

## Fixes

* In HelloCube_06_SpawnFromEntity, `HelloSpawnerSystem` now delays spawning entities until the beginning of the next simulation group update. This ensures
  that spawned entities are fully instantiated and processed by the `TransformSystemGroup` before they are rendered for the first time.
* Fixed exception in inspector when Script is missing
* Fixed issue where the presence of chunk components could lead to corruption of the entity remapping during deserialization of SubScene sections.
* Fix for an issue causing filtering with `IJobForEachWithEntity` to try to access entities outside of the range of the group it was scheduled with.

## Known issues

# 0.0.26

## New Features

## Upgrade guide

## Changes

* More improvements to the documentation

## Fixes

* Change filtering with two component types now works again.

## Known issues

# 0.0.25

## New Features

* Added BlobAssetReference<T> and support for building and serializing blob assets.
  * Blob assets are built using BlobAllocator and BlobAssetReference fields in components are automatically serialized and deserialized.
  * BlobPtr and BlobArray are used to represent pointers and arrays inside blobs and are allocated using BlobAllocator.Allocate.
  * BlobAssetReference are currently not supported inside DynamicBuffer components.
* bool and char can now be used in ComponentData and in native collections.
* GetBufferFromEntity and GetComponentDataFromEntity only available on JobComponentSystem

## Upgrade guide

* Unity 2019.1b5 or later is now required.

## Changes

* If a system in a ComponentSystemGroup throws an exception, the group will now log the exception as an error and continue updating the
  next system in the group. Previously, the entire group update would abort.
* Moved most documentation to the [Entities](https://docs.unity3d.com/Packages/com.unity.entities@0.0/manual/index.html), [Collections extensions](https://docs.unity3d.com/Packages/com.unity.collections@0.0/manual/index.html), and [Job extensions](https://docs.unity3d.com/Packages/com.unity.jobs@0.0/manual/index.html) packages.

## Fixes

* Fix underconstrained systems in the `TransformSystemGroup`, which could cause child transforms to lag
  behind their parents for one frame.

## Known issues

# 0.0.24

## New Features

* New ["Fluent"](https://en.wikipedia.org/wiki/Fluent_interface) API for more easily tuning the query used in a `ForEach` or even constructing a `ComponentGroup`.
  * To use, try the `Entities` property of `ComponentSystem`, which returns a `EntityQueryBuilder` that has a set of `With` methods on it to construct a query.
  * `ForEach` now lives on this and the parameters of the lambda will be combined with the builder to form the final cached `ComponentGroup`.
  * You can also call `ToComponentGroup` from the builder if you just want to use this new way of constructing one.
  * The default cache size of 10 for `EntityQueryBuilder`-created queries can be tuned with `InitEntityQueryCache`.

## Upgrade guide

* Fluent `ForEach` changes: typically you can just insert `Entities.` in front of your existing `ForEach` calls. If you are passing in a group, then change
`ForEach(..., group)` to `Entities.With(group).ForEach(...)`.

## Changes

* Top-level `ComponentSystemGroup`s in the default world are now updated from different stages of the player loop. This is a temporary workaround;
  see [forum post](https://forum.unity.com/threads/why-is-simulation-the-default-system-group.639058/#post-4289911) for more details.
  * `SimulationSystemGroup` is now updated at the end of the `Update` phase of the player loop, not `FixedUpdate`. As a side effect, it will now be updated
  once per rendered frame, with a variable timestemp in `Time.deltaTime`.
  * `PresentationSystemGroup` is now updated at the end of the `PreLateUpdate` phase of the player loop, not `Update`.

## Fixes

* Fix for case 1119844: [IL2CPP] Null Exception is thrown when passing NativeList to a IJobParallellFor
* Systems in the `LateSimulationSystemGroup` are now sorted properly based on their ordering constraints.
* Corrected loop bounds in `HelloSpawnerSystem`.
* `CreateManager(Type t)` now throws an error if `t` is not derived from `ScriptBehaviourManager`.
* `[UpdateInGroup(G)]` will now fail more obviously if type `G` is not derived from `ComponentSystemGroup`.
  * A warning will also be logged if `G` throws an error in its constructor, indicating that construction
    of member systems will be skipped.
* `ComponentSystemGroup` sort order is now deterministic, for a given set of systems and ordering constraints.

## Known issues

# 0.0.23

## New Features

* Added ComponentGroup versions of AddChunkComponentData, RemoveChunkComponentData and AddSharedComponentData. Also optimized the ComponentGroup versions of AddComponent and RemoveComponent.
These can now be used to add and remove components to all the chunks/entities in an ComponentGroup. For components that don't change the layout of chunks (Tag, Shared and Chunk components)
these functions will take an optimized path that migrates the chunks to new archetypes without copying the component data.
* Added new transform-related components as part of redesign (in progress)
  * `CompositeRotation`
    * `PreRotation`
    * `PreRotationEulerXYZ`…`ZYX`
    * `RotationEulerXYZ`…`ZYX`
    * `RotationPivot`
    * `RotationPivotTranslation`
  * `CompositeScale`
    * `Scale` (now for uniform scale)
    * `ScalePivot`
    * `ScalePivotTranslation`

## Upgrade guide

## Changes

* 2018.3 support has been dropped. Please ensure you are on 2019.1+ if you want to keep getting new updates
* BarrierSystem renamed to EntityCommandBufferSystem
* Subtractive renamed to Exclude
  * `[RequireSubtractiveComponent]` renamed to `[ExcludeComponent]`
* ComponentType.Create renamed to ComponentType.ReadWrite
* Transform-related component names changes
  * `Position`-\>`Translation`
  * `Scale`-\>`NonUniformScale`
* Attach, Attached components removed. (No transform hierarchy)
* `ComponentDataArray`, `BufferArray`, `SharedComponentDataArray`, and `EntityArray` have been deprecated. Please use `ForEach`, `IJobProcessComponentData`, `IJobChunk`, and `ComponentGroup` APIs to access component data.
* `[Inject]` has been deprecated. See [the Injection documentation](Documentation/content/injection.md) for more information.
* Component system update ordering is now hierarchical. A forthcoming document will cover this feature in detail. Key changes:
  * Added `ComponentSystemGroup` class, representing a group of systems (and system groups) to update in a fixed order.
  * The following `ComponentSystemGroup`s are added to the Unity player loop by default:
    * `InitializationSystemGroup` (in the `Initialization` phase)
    * `SimulationSystemGroup` (in the `FixedUpdate` phase)
    * `PresentationSystemGroup` (in the `Update` phase)
  * Each of the default system groups contains a pair of `BarrierSystem`s which run at the beginning and end of that group (e.g. `EndSimulationBarrier`).
    * `EndFrameBarrier` has been removed; use the `End` barrier in the appropriate system group instead.
  * Use `[UpdateInGroup]` to specify which `ComponentSystemGroup` a system should be added to during default world initialization.
    * If omitted, systems are added to the `SimulationSystemGroup` by default (and will thus update during the FixedUpdate phase).
    * Built-in ECS systems have been pre-assigned to the appropriate groups.
  * Use `[UpdateBefore]` and `[UpdateAfter]` to specify relative ordering of systems within their common `ComponentSystemGroup`.
    * Ordering relative to systems in different system groups is implicit from the group hierarchy; explicitly specifying this ordering triggers a warning and will be ignored.
  * Added `ICustomBootstrap` interface to allow applications to partially/fully override the default world initialization process, support multiple Worlds, etc.
* MoveEntitiesFrom no longer remaps Entity references.

## Fixes

* Fixed bug causing incorrect read dependency error on Unity.Entities.Entity
* ComponentSystem.GetComponentGroup(...) will no longer treat two queries with the same component types and access modes, but in a different order as different groups.

## Known issues

# 0.0.22

## New Features

* Added DynamicBufferProxy base class to allow authoring DynamicBuffer data in hybrid mode (similar to ComponentDataProxy).
* EntityManager.AddComponentObject and EntityManager.GetComponentObject let you attach UnityEngine.Component based classes to an entity.
* Added CopyFromComponentDataArray API to ComponentGroup for easy write-back of data to chunks from a NativeArray

## Upgrade guide

* EntityCommandBuffer functions that implicitly operate on the most recently created/instantiated Entity are now deprecated
  in favor of the variants that take an explicit Entity parameter.
* EntityManager.CreateArchetypeChunkArray() is now deprecated; use ComponentGroup.CreateArchetypeChunkArray() instead.
* EntityManager.AddMatchingArchetypes() is now deprecated. No direct alternative is available.
* ComponentDataWrapper and SharedComponentDataWrapper have been renamed to ComponentDataProxy and SharedComponentDataProxy
  * Hybrid MonoBehaviours XComponent have been renamed to XProxy throughout

## Changes

* EntityDebugger and Entity inspector API are no longer public
* Transform-related component names changes (e.g. Position-\>Translation, Rotation-\>RotationQuaternion, Scale-\>ScaleXYZ)
* Attach, Attached components removed. (No transform hierarchy)
* The Containers package no longer depends on the Math package

## Fixes

* Fix to a bug in ComponentChunkIterator which causes incorrect calculation of entity offset while filtering by a shared component data value (affected both IJobChunk and IJobProcessComponentData)
* Fix IJobProcessComponentData.ScheduleGroupSingle & RunGroup to not use parallel scheduling codepath. Thus able to write to full range of arrays or command buffers.
* HybridSerializeUtility no longer has an implicit naming convention requirement for SharedComponentDataProxy (i.e. wrapper) classes.
* NativeString truncations no longer assert, but return error codes
* World Diff now respects semantics of Prefab and LinkedEntityGroup components

## Known issues

* Playmode tests sometimes crashes for 2018.3 on windows

# 0.0.21

## New Features

* Added new batched renderer for MegaCity sample (Requires API in 19.1 and is disabled by default)
* Added custom job type to schedule parallel jobs over hash map.
* Added DynamicBuffer accessor to ExclusiveEntityTransaction.
* ComponentSystem.GetSingleton / ComponentSystem.SetSingleton to simplify access to singleton data
* ComponentSystem.RequireForUpdate(ComponentGroup) for specifiying ComponentGroups that are required for a system to run.
* GameObjectConversionSystem & GameObjectConversionUtility can be used to perform conversion of existing GameObject scenes into entity representation.
* WorldDiff can be used to create a diff between a previously applied state of the world.
* GameObjectConversionSystem & WorldDiff combined are the foundation for scene management tools & live pipeline tools that are in progress and not yet part of this.
* NativeHashMap.GetKeyArray lets you retrieve all keys of the HashMap into a NativeArray
* Added support for chunk components. Chunk components are like ordinary components except they can be added to and accessed by ArchetypeChunks.

## Upgrade guide

* Much of the rendering logic has been moved to a separate package called Hybrid Renderer (com.unity.rendering.hybrid). So in custom projects you are likely to need to now add the Hybrid Renderer in the Package Manager window.
  * Note especially that the `MeshInstanceRenderer` component has been renamed to `RenderMesh`, and is part of the new Hybrid Renderer package.
* Systems that use BarrierSystem.CreateCommandBuffer() to create an EntityCommandBuffer and record that command buffer in a job must now call BarrierSystem.AddJobHandleForProducer() to ensure that the recording job completes before the barrier initiates command buffer playback.

## Changes

* Systems that should execute in edit mode should now use the ExecuteAlways attribute instead of ExecuteInEditMode.
* ISerializationCallbackReceiver interfaces on ComponentDataWrapperBase are no longer public.
* IJobProcessComponentData now supports up to 6 separate IComponentData
* TypeManager.BuildComponentType() has been made internal.
* EntityManager.MoveEntitiesFrom now supports shared components with entity references

## Fixes

* Fixed null exception arising when entering prefab editing mode during play mode, and then exiting play mode while still in prefab editing mode (Fogbugz case 1091596).
* Fixed null exception when resetting a ComponentDataWrapper via the context menu in the Inspector.

# 0.0.20

## New Features

## Upgrade guide

## Changes

* ComponentGroup.Types was made internal, since it will likely be refactored
* Renamed DynamicBuffer.GetBasePointer() to DynamicBuffer.GetUnsafePtr() for consistency with NativeArray API.

## Fixes

* Component types no longer show up multiple times in archetypes and queries in the EntityDebugger
* Attempting to rename an entity in the EntityDebugger no longer causes an exception (naming is not yet supported)
* Component filter editor now shows types in consistent alphabetical order when filtering
* The Show button in the GameObjectEntity inspector works again
* Fixed [ReadOnly] DynamicBuffer incorrectly throwing exceptions when getting the NativeArray inside of a job.
* Fixed DynamicBuffer not throwing the right exceptions when writing to buffers while NativeArrays are being used in jobs

# 0.0.19

## New Features

## Upgrade guide

## Changes

* Updated burst to 0.2.4-preview.37 (fixes a crash in the editor when trying to load burst-llvm (mac + linux)
* Reduced redundant repaints of the EntityDebugger and inspectors.
* Documentation cleanup (broken links, additional resources, etc.)
* Internal project name "Capsicum" removed from documentation and replaced by "Data Oriented Tech Stack".

## Fixes

* Fixed Instantiate and Delete on concurrent entity command buffers.

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
* Removed sync point in GetComponentGroup resulting in two IJobProcessComponentData in the same system to fail on first execution.
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
  * Left click to select planets of the same color
  * Right click to send ships from the selected planets to the planet under the mouse
  * Can be set to play by itself if running the SceneSwitcher scene
* GravityDemo sample added
  * Press 1-7 on the keyboard while it's running to change to different simulations
  * Left click to spawn new asteroids from the camera
  * While holding right click, move the mouse and use AWSD buttons to control the camera

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
  * MeshLODGroupComponent defines the lod sizes and active lod.
  * MeshLODComponent references an Entity with a MeshLODGroupComponent and enables / disables itself based on the specified active lod. Transforms between mesh and group must match.
* Entity worlds can now be serialized and deserialized to/from a binary format using SerializeUtility
  * Use SerializeUtilityHybrid to support shared components

## Changes

* EntityDebugger's display of ComponentGroups is improved:
  * They will now wrap to multiple lines if there isn't enough space
  * Generic types are displayed nicely
  * Sort order is stable

# 0.0.6

## New Features

* OnStartRunning() and OnStopRunning() added to ComponentSystem and JobComponentSystem
  * OnStartRunning is called when a system's Enabled or ShouldStartRunning() becomes true
  * OnStopRunning is called when a system's Enabled or ShouldStartRunning() becomes false. Also when the system will get destroyed.
  * It will only send one of each in succession
    * Example: Two OnStartRunning() cannot be triggered for a given system without an OnStopRunning() call in between
* Experimental SOA containers updated, now split into two different types:
  * NativeArrayFullSOA internally lays everything out in sub-arrays
  * NativeArrayChunked8 internally lays data out in chunks of 32 bytes
* Component type versions in Chunks (for broadphase change tracking)
* Query Archetype and Chunk iteration (query archetypes matching all/any/none component filter, and e.g. allow component existence checks on chunk level.)
* Add SystemStateComponentData (answer to Reactive system for add/delete components)
* IComponentSystemPatch to auto run ComponentSystem[Job] after every ComponentSystem.

## Changes

* Make it possible to create EntityArray in addition to ComponentDataArray with the new ForEachFilter

# 0.0.5

## New Features

* New API for faster filtering when going through all unique shared component values.
  * `var filter = group.CreateForEachFilter(uniqueTypes);`
  * `var array = group.GetComponentDataArray<Type>(filter, i); // in a loop`
  * `filter.Dispose();`

## Changes

* Throw ArgumentException when creating an entity with component data exceeding chunk size (64kb)
* EntityManager.CreateComponentGroup is no longer public, use ComponentSystem.GetComponentGroup instead
* Fix an incorrect hash calculation when resizing a HashMap

# 0.0.4

## New Features

* New Entity Debugger replaces EntityWindow and SystemWindow
  * Lists Systems, allowing you to browse the Entities in each of their ComponentGroups
  * Systems that are not running due to empty ComponentGroups will appear greyed out
  * Systems can be enabled and disabled temporarily for testing purposes
  * System main thread time is shown. Job time is not currently exposed (the Profiler is a more robust tool for this)
  * Selecting an Entity will show it in the inspector. This support is rudimentary, but will improve soon.

## Changes

* ComponentGroup.GetVariant replaced by ComponentGroup.SetFilter. The ComponentGroup is reused and simply chnages the filter on this ComponentGroup.
  * Reduces GC allocations, since only one ComponentGroup will ever be created.
  * Fixes bug where shared component data indices would go out of sync when used on a job.
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
  `SharedComponentDataArray<T>` can also be injected similar to `ComponentDataArray<T>`
  Access through SharedComponentDataArray is always read only

* IJobProcessComponentData is significantly simplified. Supports 1, 2, 3 parameters. Supports read only, supports additional required components & subtractive components. [See source for RotationSpeedSystem.cs](https://github.com/Unity-Technologies/ECSJobDemos/blob/stable/ECSJobDemos/Assets/GameCode/SimpleRotation/RotationSpeedSystem.cs)

# 0.0.2

## New Features

## Changes

* [InjectComponentGroup] and [InjectComponentFromEntity] were replaced by simply [Inject] handling all injection cases.
* EntityManager component naming consistency renaming
  EntityManager can access both components and component data thus:
  * HasComponent(ComponentType type), RemoveComponent(ComponentType type), AddComponent(ComponentType type)
  * AddComponentData(Entity entity, T componentData) where T : struct, IComponentData

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
