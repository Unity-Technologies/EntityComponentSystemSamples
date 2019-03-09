# Unity Data-Oriented Tech Stack Cheat Sheet

Here is a quick reference of the most useful classes, interfaces, structs, and attributes that have been introduced in this documentation by [ECS](#ecs-related), the [C# Job System](#c-job-system-related), and the [Burst compiler](#burst-compiler-related). 

> **Note**: This is not an exhaustive list and can be added to over time as the Unity Data-Oriented Tech Stack, and its related documentation, expands. Check the [repository code](https://github.com/Unity-Technologies/EntityComponentSystemSamples) and the [Scripting API](https://docs.unity3d.com/ScriptReference/) under the namespaces mentioned below for more examples. Be aware that links can break as the code evolves, so if you notice a problem let us know in the [forums](http://www.unity3d.com/performance-by-default) or as an [issue](https://github.com/Unity-Technologies/EntityComponentSystemSamples/issues/new) in the repository.

## ECS related

| Name     | Path   | Type  |
| :-------------: |:-------------| :-----:|
| CopyInitialTransformFromGameObjectComponent | /Packages/com.unity.entities/Unity.Transforms.Hybrid/CopyInitialTransformFromGameObjectComponent.cs |     Class      |
| Chunk | /Packages/com.unity.entities/Unity.Entities/ArchetypeManager.cs | Unsafe Struct |
| ComponentDataArray | /Packages/com.unity.entities/Unity.Entities/Iterators/ComponentDataArray.cs | Unsafe Struct |
| ComponentDataFromEntity | /Packages/com.unity.entities/Unity.Entities/Iterators/ComponentDataFromEntity.cs | Unsafe Struct |
| ComponentGroup | /Packages/com.unity.entities/Unity.Entities/Iterators/ComponentGroup.cs | Unsafe Class |
| ComponentSystem | /Packages/com.unity.entities/Unity.Entities/ComponentSystem.cs | Abstract Class |
| ComponentType | /Packages/com.unity.entities/Unity.Entities/Types/ComponentType.cs | Struct |
| DynamicBuffer | /Packages/com.unity.entities/Unity.Entities/Iterators/DynamicBuffer.cs | Unsafe Struct |
| Entity | /Packages/com.unity.entities/Unity.Entities/EntityManager.cs | Struct |
| EntityArchetype | /Packages/com.unity.entities/Unity.Entities/EntityManager.cs | Unsafe Struct |
| EntityCommandBuffer | /Packages/com.unity.entities/Unity.Entities/EntityCommandBuffer.cs | Unsafe Struct |
| EntityManager | /Packages/com.unity.entities/Unity.Entities/EntityManager.cs | Unsafe Class |
| ExclusiveEntityTransaction | /Packages/com.unity.entities/Unity.Entities/ExclusiveEntityTransaction.cs | Unsafe Struct |
| GameObjectEntity | /Packages/com.unity.entities/Unity.Entities.Hybrid/GameObjectEntity.cs | Class |
| IComponentData | /Packages/com.unity.entities/Unity.Entities/IComponentData.cs | Interface |
| IJobParallelForBatch | /Packages/com.unity.jobs/Unity.Jobs/IJobParallelForBatch.cs) | Interface |
| IJobParallelForFilter | /Packages/com.unity.jobs/Unity.Jobs/IJobParallelForFilter.cs | Interface |
| IJobProcessComponentData | /Packages/com.unity.entities/Unity.Entities/IJobProcessComponentData.cs | Interface |
| ISharedComponentData | /Packages/com.unity.entities/Unity.Entities/IComponentData.cs | Interface |
| JobComponentSystem | /Packages/com.unity.entities/Unity.Entities/ComponentSystem.cs | Abstract Class |
| MeshInstanceRendererComponent | /Packages/com.unity.entities/Unity.Rendering.Hybrid/MeshInstanceRendererComponent.cs | Class |
| NativeHashMap | /Packages/com.unity.collections/Unity.Collections/NativeHashMap.cs | Unsafe Struct |
| NativeList | /Packages/com.unity.collections/Unity.Collections/NativeList.cs | Unsafe Struct |
| NativeQueue | /Packages/com.unity.collections/Unity.Collections/NativeQueue.cs | Unsafe Struct |
| PositionComponent | /Packages/com.unity.entities/Unity.Transforms/PositionComponent.cs | Class |
| TransformSystem | /Packages/com.unity.entities/Unity.Transforms/TransformSystem.cs | Class |
| World | /Packages/com.unity.entities/Unity.Entities/Injection/World.cs | Class |

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