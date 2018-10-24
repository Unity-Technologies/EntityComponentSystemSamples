# Capsicum cheat sheet

Here is a quick reference of the most useful classes, interfaces, structs, and attributes that have been introduced in this documentation by [ECS](#ecs-related), the [C# Job System](#c-job-system-related), and the [Burst compiler](#burst-compiler-related). 

> **Note**: This is not an exhaustive list and can be added to over time as Capsicum, and its related documentation, expands. Check the [repository code](https://github.com/Unity-Technologies/EntityComponentSystemSamples) and the [Scripting API](https://docs.unity3d.com/ScriptReference/) under the namespaces mentioned below for more examples. Be aware that links can break as the code evolves, so if you notice a problem let us know in the [forums](http://www.unity3d.com/performance-by-default) or as an [issue](https://github.com/Unity-Technologies/EntityComponentSystemSamples/issues/new) in the repository.

## ECS related

| Namespace     | Name          | Type  |
| :-------------: |:-------------:| :-----:|
| Unity.Collections | [NativeHashMap](../../Samples/Packages/com.unity.collections/Unity.Collections/NativeHashMap.cs) | Unsafe Struct |
| Unity.Collections | [NativeList](../../Samples/Packages/com.unity.collections/Unity.Collections/NativeList.cs) | Unsafe Struct |
| Unity.Collections | [NativeQueue](../../Samples/Packages/com.unity.collections/Unity.Collections/NativeQueue.cs) | Unsafe Struct |
| Unity.Entities | [Chunk](../../Samples/Packages/com.unity.entities/Unity.Entities/ArchetypeManager.cs) | Unsafe Struct |
| Unity.Entities | [ComponentDataArray](../../Samples/Packages/com.unity.entities/Unity.Entities/Iterators/ComponentDataArray.cs) | Unsafe Struct |
| Unity.Entities | [ComponentDataFromEntity](../../Samples/Packages/com.unity.entities/Unity.Entities/Iterators/ComponentDataFromEntity.cs) | Unsafe Struct |
| Unity.Entities | [ComponentGroup](../../Samples/Packages/com.unity.entities/Unity.Entities/Iterators/ComponentGroup.cs) | Unsafe Class |
| Unity.Entities | [ComponentSystem](../../Samples/Packages/com.unity.entities/Unity.Entities/ComponentSystem.cs) | Abstract Class |
| Unity.Entities | [ComponentType](../../Samples/Packages/com.unity.entities/Unity.Entities/Types/ComponentType.cs) | Struct |
| Unity.Entities | [DynamicBuffer](../../Samples/Packages/com.unity.entities/Unity.Entities/Iterators/DynamicBuffer.cs) | Unsafe Struct |
| Unity.Entities | [Entity](../../Samples/Packages/com.unity.entities/Unity.Entities/EntityManager.cs) | Struct |
| Unity.Entities | [EntityArchetype](../../Samples/Packages/com.unity.entities/Unity.Entities/EntityManager.cs) | Unsafe Struct |
| Unity.Entities | [EntityCommandBuffer](../../Samples/Packages/com.unity.entities/Unity.Entities/EntityCommandBuffer.cs) | Unsafe Struct |
| Unity.Entities | [EntityManager](../../Samples/Packages/com.unity.entities/Unity.Entities/EntityManager.cs) | Unsafe Class |
| Unity.Entities | [ExclusiveEntityTransaction](../../Samples/Packages/com.unity.entities/Unity.Entities/ExclusiveEntityTransaction.cs) | Unsafe Struct |
| Unity.Entities | [GameObjectEntity](../../Samples/Packages/com.unity.entities/Unity.Entities.Hybrid/GameObjectEntity.cs) | Class |
| Unity.Entities | [IComponentData](../../Samples/Packages/com.unity.entities/Unity.Entities/IComponentData.cs) | Interface |
| Unity.Entities | [IJobProcessComponentData](../../Samples/Packages/com.unity.entities/Unity.Entities/IJobProcessComponentData.cs) | Interface |
| Unity.Entities | [ISharedComponentData](../../Samples/Packages/com.unity.entities/Unity.Entities/IComponentData.cs) | Interface |
| Unity.Entities | [JobComponentSystem](../../Samples/Packages/com.unity.entities/Unity.Entities/ComponentSystem.cs) | Abstract Class |
| Unity.Entities | [World](../../Samples/Packages/com.unity.entities/Unity.Entities/Injection/World.cs) | Class |
| Unity.Jobs | [IJobParallelForBatch](../../Samples/Packages/com.unity.jobs/Unity.Jobs/IJobParallelForBatch.cs) | Interface |
| Unity.Jobs | [IJobParallelForFilter](../../Samples/Packages/com.unity.jobs/Unity.Jobs/IJobParallelForFilter.cs) | Interface |
| Unity.Rendering | [MeshInstanceRendererComponent](../../Samples/Packages/com.unity.entities/Unity.Rendering.Hybrid/MeshInstanceRendererComponent.cs) | Class |
| Unity.Transforms | [PositionComponent](../../Samples/Packages/com.unity.entities/Unity.Transforms/PositionComponent.cs) | Class |
| Unity.Transforms | [CopyInitialTransformFromGameObjectComponent](../../Samples/Packages/com.unity.entities/Unity.Transforms.Hybrid/CopyInitialTransformFromGameObjectComponent.cs) | Class |
| Unity.Transforms | [TransformSystem](../../Samples/Packages/com.unity.entities/Unity.Transforms/TransformSystem.cs) | Class |

### Attributes

* [Inject]

#### Unsafe attributes 

* [[NativeContainer]](https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerAttribute.html)
* [[NativeContainerIsAtomicWriteOnly]](https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerIsAtomicWriteOnlyAttribute.html) 
* [[NativeContainerSupportsMinMaxWriteRestriction]](https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerSupportsMinMaxWriteRestrictionAttribute.html) 
* [[NativeContainerSupportsDeallocateOnJobCompletion]](https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerSupportsDeallocateOnJobCompletionAttribute.html)
* [[NativeDisableUnsafePtrRestriction]](https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestrictionAttribute.html)
* [[NativeSetClassTypeToNullOnSchedule]](https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeSetClassTypeToNullOnScheduleAttribute.html)

### Other

* \#if ENABLE_UNITY_COLLECTIONS_CHECKS ... #endif

## C# Job System related

> **Note**: ECS code can also use the following objects, but they are part of the  Unity codebase since 2018.1 and not part of any related packages. For more information, see the [C# Job System manual](https://docs.unity3d.com/Manual/JobSystem.html).

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
* [BurstCompile]