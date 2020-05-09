# Unity Data-Oriented Tech Stack Cheat Sheet

Here is a quick reference of the most useful classes, interfaces, structs, and attributes that have been introduced in this documentation by [ECS](#ecs-related), the [C# Job System](#c-job-system-related), and the [Burst compiler](#burst-compiler-related).

## ECS related

| Name     | Namespace   | Type  |
| :-------------: |:-------------| :-----:|
| [ArchetypeChunk](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Entities.ArchetypeChunk.html) | Unity.Entities | Unsafe Struct |
| [ComponentSystem](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Entities.ComponentSystem.html) | Unity.Entities | Abstract Class |
| [ComponentType](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Entities.ComponentType.html) | Unity.Entities | Struct |
| [DynamicBuffer](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Entities.DynamicBuffer-1.html) | Unity.Entities | Unsafe Struct |
| [Entity](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Entities.Entity.html) | Unity.Entities | Struct |
| [EntityArchetype](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Entities.EntityArchetype.html) | Unity.Entities | Unsafe Struct |
| [EntityCommandBuffer](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Entities.EntityCommandBuffer.html) | Unity.Entities | Unsafe Struct |
| [EntityManager](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Entities.EntityManager.html) | Unity.Entities | Unsafe Class |
| [ExclusiveEntityTransaction](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Entities.ExclusiveEntityTransaction.html) | Unity.Entities | Unsafe Struct |
| [IBufferElementData](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Entities.IBufferElementData.html) | Unity.Entities | Interface |
| [IComponentData](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Entities.IComponentData.html) | Unity.Entities | Interface |
| [IJobChunk](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Entities.IJobChunk.html) | Unity.Entities | Interface |
| [IJobNativeMultiHashMapMergedSharedKeyIndices](https://docs.unity3d.com/Packages/com.unity.collections@latest?preview=1&subfolder=/api/Unity.Collections.IJobNativeMultiHashMapMergedSharedKeyIndices.html) | Unity.Collections | Interface |
| [IJobParallelForBatch](https://docs.unity3d.com/Packages/com.unity.jobs@latest?preview=1&subfolder=/api/Unity.Jobs.IJobParallelForBatch.html) | Unity.Jobs | Interface |
| [IJobParallelForFilter](https://docs.unity3d.com/Packages/com.unity.jobs@latest?preview=1&subfolder=/api/Unity.Jobs.IJobParallelForFilter.html) | Unity.Jobs | Interface |
| [IJobForEach](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Entities.IJobForEach-1.html) | Unity.Entities | Interface |
| [ISharedComponentData](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Entities.ISharedComponentData.html) | Unity.Entities | Interface |
| [ISystemStateBufferElementData](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Entities.ISystemStateBufferElementData.html) | Unity.Entities | Interface |
| [ISystemStateComponentData](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Entities.ISystemStateComponentData.html) | Unity.Entities | Interface |
| [ISystemStateSharedComponentData](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Entities.ISystemStateSharedComponentData.html) | Unity.Entities | Interface |
| [JobComponentSystem](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Entities.JobComponentSystem.html) | Unity.Entities | Abstract Class |
| [RenderMesh](https://docs.unity3d.com/Packages/com.unity.rendering.hybrid@latest?preview=1&subfolder=/api/Unity.Rendering.RenderMesh.html) |Unity.Rendering | Class |
| [NativeHashMap](https://docs.unity3d.com/Packages/com.unity.collections@latest?preview=1&subfolder=/api/Unity.Collections.NativeHashMap-2.html) | Unity.Collections | Unsafe Struct |
| [NativeList](https://docs.unity3d.com/Packages/com.unity.collections@latest?preview=1&subfolder=/api/Unity.Collections.NativeList-1.html) | Unity.Collections | Unsafe Struct |
| [NativeMultiHashMap](https://docs.unity3d.com/Packages/com.unity.collections@latest?preview=1&subfolder=/api/Unity.Collections.NativeMultiHashMap-2.html) | Unity.Collections | Unsafe Struct |
| [NativeQueue](https://docs.unity3d.com/Packages/com.unity.collections@latest?preview=1&subfolder=/api/Unity.Collections.NativeQueue-1.html) | Unity.Collections | Unsafe Struct |
| [LocalToWorld](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Transforms.LocalToWorld.html) | Unity.Transforms | Struct |
| [Translation](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Transforms.Translation.html) | Unity.Transforms | Struct |
| [Rotation](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Transforms.Rotation.html) | Unity.Transforms | Struct |
| [Scale](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Transforms.Scale.html) | Unity.Transforms | Struct |
| [World](https://docs.unity3d.com/Packages/com.unity.entities@latest?preview=1&subfolder=/api/Unity.Entities.World.html) | Unity.Entities | Class |

### Unsafe attributes

* [[NativeContainer]](https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerAttribute.html)
* [[NativeContainerIsAtomicWriteOnly]](https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerIsAtomicWriteOnlyAttribute.html)
* [[NativeContainerSupportsMinMaxWriteRestriction]](https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerSupportsMinMaxWriteRestrictionAttribute.html)
* [[NativeContainerSupportsDeallocateOnJobCompletion]](https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerSupportsDeallocateOnJobCompletionAttribute.html)
* [[NativeDisableUnsafePtrRestriction]](https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestrictionAttribute.html)
* [[NativeSetClassTypeToNullOnSchedule]](https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeSetClassTypeToNullOnScheduleAttribute.html)

### Defines

Frequently used:

* `ENABLE_UNITY_COLLECTIONS_CHECKS`: Wrap around validation code, for example bounds checking, parameter validation, or leak detection.
* `NET_DOTS`: DOTS C# profile (vs full .NET) - for example, if you use `Dictionary`, you want `#if !NET_DOTS`; see [Platform dependent compilation](https://docs.unity3d.com/Manual/PlatformDependentCompilation.html) for the other `NET_*` defines
* `UNITY_DOTSPLAYER`: Standalone DOTS (vs hybrid DOTS) - for example, if you use `UnityEngine`, you want `#if !UNITY_DOTSPLAYER`

Rarely used:

* `UNITY_DOTSPLAYER_DOTNET`: Standalone DOTS built for real .NET execution target
* `UNITY_DOTSPLAYER_IL2CPP`: Standalone DOTS built for IL2CPP - for example, where il2cpp provides an intrinsic for vs. an implementation using reflection in .NET

_Note: The `ZEROPLAYER` defines are for the DOTS runtime, which has not been publicly released yet._

## C# Job System related

> **Note**: ECS code can also use the following objects, but they are part of the Unity codebase since 2018.1 and not part of any related packages. For more information, see the [C# Job System manual](https://docs.unity3d.com/Manual/JobSystem.html).

| Namespace     | Name          | Type  |
| :-------------: |:-------------:| :-----:|
| Unity.Collections | [NativeArray](https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeArray_1.html)  | Struct |
| Unity.Collections | [NativeContainer](https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerAttribute.html) | Unsafe Class |
| Unity.Collections | [NativeSlice](https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeSlice_1.html) | Struct |
| Unity.Jobs | [IJob](https://docs.unity3d.com/ScriptReference/Unity.Jobs.IJob.html) | Interface |
| Unity.Jobs | [IJobParallelFor](https://docs.unity3d.com/ScriptReference/Unity.Jobs.IJobParallelFor.html) | Interface |
| Unity.Jobs | [JobHandle](https://docs.unity3d.com/ScriptReference/Unity.Jobs.JobHandle.html) | Interface |
| Unity.Jobs | [JobsUtility](https://docs.unity3d.com/ScriptReference/Unity.Jobs.LowLevel.Unsafe.JobsUtility.html) | Unsafe Class |

### Attributes

* [[ReadOnly]](https://docs.unity3d.com/ScriptReference/Unity.Collections.ReadOnlyAttribute.html)
* [[WriteOnly]](https://docs.unity3d.com/ScriptReference/Unity.Collections.WriteOnlyAttribute.html)

## Burst compiler related

### Attributes

* [[BurstDiscard]](https://docs.unity3d.com/ScriptReference/Unity.Burst.BurstDiscardAttribute.html)
* [[BurstCompile]](https://docs.unity3d.com/Packages/com.unity.burst@latest?preview=1&subfolder=/api/Unity.Burst.BurstCompileAttribute.html)

## General computing terms

* [AOT compilation](glossary.md#aot_compilation)
* [Atomic operation](glossary.md#atomic_operation)
* [Blittable types](glossary.md#blittable_types)
* [Cache lines](https://en.wikipedia.org/wiki/CPU_cache#Cache_entries)
* [Context switching](https://docs.unity3d.com/Manual/JobSystemMultithreading.html) - see the end of the page.
* [Dependency](glossary.md#dependency)
* [JIT compilation](glossary.md#jit_compilation)
* [Job system](https://docs.unity3d.com/Manual/JobSystemJobSystems.html)
* [Logical CPU](glossary.md#logical_cpu)
* [Main thread](glossary.md#main_thread)
* [Managed code](glossary.md#managed_code)
* [Memory leak](glossary.md#memory_leak)
* [Multicore](glossary.md#multicore)
* [Multithreading](https://docs.unity3d.com/Manual/JobSystemMultithreading.html)
* [Native code](https://en.wikipedia.org/wiki/Machine_code)
* [Parallel computing](https://en.wikipedia.org/wiki/Parallel_computing)
* [Performant](glossary.md#performant)
* [Race condition](https://docs.unity3d.com/Manual/JobSystemSafetySystem.html)
* [SIMD](glossary.md#simd)
* [Unmanaged code](glossary.md#unmanaged_code)
* [Worker threads](glossary.md#worker_threads)
