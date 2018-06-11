# ECS Cheat Sheet

Here is a quick reference of the common classes, interfaces, structs, and attributes that have been introduced in this documentation by [ECS](#ecs-related), [Burst compiler](#burst-compiler-related) and the [C# job system](#c#-job-system-related). Click the links below for more information from the Unity Manual and Scripting API.

> Note: This is not an exhaustive list and can be added to over time as ECS, and its related documentation, expands. Check the ECSJobDemo code and the [Scripting API]() under the namespaces mentioned below for more examples.

## C# job system related

> Note: These can also be used by ECS code, but they are part of the main Unity 2018.1 release and not part of the ECS packages.

| Namespace     | Name          | Type  |
| :-------------: |:-------------:| :-----:|
| Unity.Collections | [NativeArray](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.NativeArray_1.html)  | Struct |
| Unity.Collections | [NativeContainer](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerAttribute.html) | Unsafe Class | 
| Unity.Collections | [NativeSlice](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.NativeSlice_1.html) | Struct | 
| Unity.Jobs | [IJob](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Jobs.IJob.html) | Interface | 
| Unity.Jobs | [IJobParallelFor](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Jobs.IJobParallelFor.html) | Interface |
| Unity.Jobs | [JobHandle](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Jobs.JobHandle.html) | Interface |
| Unity.Jobs | [JobsUtility](https://docs.unity3d.com/es/2018.1/ScriptReference/Unity.Jobs.LowLevel.Unsafe.JobsUtility.html) | Unsafe Class |

### Attributes

* [[ReadOnly]](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.ReadOnlyAttribute.html)
* [[WriteOnly]](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.WriteOnlyAttribute.html)

## ECS related

| Namespace     | Name          | Type  |
| :-------------: |:-------------:| :-----:| 
| Unity.Collections | [NativeHashMap](../../ECSJobDemos/Packages/com.unity.collections/Unity.Collections/NativeHashMap.cs) | Unsafe Struct |
| Unity.Collections | [NativeList](../../ECSJobDemos/Packages/com.unity.collections/Unity.Collections/NativeList.cs) | Unsafe Struct |
| Unity.Collections | [NativeQueue](../../ECSJobDemos/Packages/com.unity.collections/Unity.Collections/NativeQueue.cs) | Unsafe Struct |
| Unity.Entities | [ComponentDataArray](../../ECSJobDemos/Packages/com.unity.entities/Unity.Entities/Iterators/ComponentDataArray.cs) | Unsafe Struct |
| Unity.Entities | [ComponentDataFromEntity](../../ECSJobDemos/Packages/com.unity.entities/Unity.Entities/Iterators/ComponentDataFromEntity.cs) | Unsafe Struct |
| Unity.Entities | [ComponentGroup](../../ECSJobDemos/Packages/com.unity.entities/Unity.Entities/Iterators/ComponentGroup.cs) | Unsafe Class |
| Unity.Entities | [ComponentSystem](../../ECSJobDemos/Packages/com.unity.entities/Unity.Entities/ComponentSystem.cs) - [ECS Docs](./getting_started.md#what-is-ecs?)  | Abstract Class |
| Unity.Entities | [ComponentType](../../ECSJobDemos/Packages/com.unity.entities/Unity.Entities/Types/ComponentType.cs)  | Struct |
| Unity.Entities | [Entity](../../ECSJobDemos/Packages/com.unity.entities/Unity.Entities/EntityManager.cs)  | Struct |
| Unity.Entities | [EntityArchetype](../../ECSJobDemos/Packages/com.unity.entities/Unity.Entities/EntityManager.cs)  | Unsafe Struct |
| Unity.Entities | [EntityCommandBuffer](../../ECSJobDemos/Packages/com.unity.entities/Unity.Entities/EntityCommandBuffer.cs)  | Unsafe Struct |
| Unity.Entities | [EntityManager](../../ECSJobDemos/Packages/com.unity.entities/Unity.Entities/EntityManager.cs)  | Unsafe Class |
| Unity.Entities | [ExclusiveEntityTransaction](../../ECSJobDemos/Packages/com.unity.entities/Unity.Entities/ExclusiveEntityTransaction.cs)  | Unsafe Struct |
| Unity.Entities | [GameObjectEntity](../../ECSJobDemos/Packages/com.unity.entities/Unity.Entities.Hybrid/GameObjectEntity.cs)  | Class |
| Unity.Entities | [IComponentData](../../ECSJobDemos/Packages/com.unity.entities/Unity.Entities/IComponentData.cs) - [ECS Docs](./ecs_in_detail.md#icomponentdata) | Interface |
| Unity.Entities | [IJobProcessComponentData](../../ECSJobDemos/Packages/com.unity.entities/Unity.Entities/IJobProcessComponentData.cs) | Interface |
| Unity.Entities | [ISharedComponentData](../../ECSJobDemos/Packages/com.unity.entities/Unity.Entities/IComponentData.cs) - [ECS Docs](./ecs_in_detail.md#shared-componentdata) | Interface |
| Unity.Entities | [JobComponentSystem](../../ECSJobDemos/Packages/com.unity.entities/Unity.Entities/ComponentSystem.cs)  | Abstract Class |
| Unity.Entities | [World](../../ECSJobDemos/Packages/com.unity.entities/Unity.Entities/Injection/World.cs) | Class |
| Unity.Jobs | [IJobParallelForBatch](../../ECSJobDemos/Packages/com.unity.jobs/Unity.Jobs/IJobParallelForBatch.cs)  | Interface |
| Unity.Jobs | [IJobParallelForFilter](../../ECSJobDemos/Packages/com.unity.jobs/Unity.Jobs/IJobParallelForFilter.cs)  | Interface |
| Unity.Rendering | [MeshInstanceRendererComponent](../../ECSJobDemos/Packages/com.unity.entities/Unity.Rendering.Hybrid/MeshInstanceRendererComponent.cs)  | Class |
| Unity.Transforms | [PositionComponent](../../ECSJobDemos/Packages/com.unity.entities/Unity.Transforms/PositionComponent.cs) | Class |
| Unity.Transforms | [CopyInitialTransformFromGameObjectComponent](../../ECSJobDemos/Packages/com.unity.entities/Unity.Transforms.Hybrid/CopyInitialTransformFromGameObjectComponent.cs) | Class |
| Unity.Transforms | [TransformMatrixComponent](../../ECSJobDemos/Packages/com.unity.entities/Unity.Transforms/TransformMatrixComponent.cs) | Class |

### Attributes

* [Inject]

#### Unsafe attributes 

* [[NativeContainer]](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerAttribute.html)
* [[NativeContainerIsAtomicWriteOnly]](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerIsAtomicWriteOnlyAttribute.html) 
* [[NativeContainerSupportsMinMaxWriteRestriction]](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerSupportsMinMaxWriteRestrictionAttribute.html) 
* [[NativeContainerNeedsThreadIndex]](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerNeedsThreadIndexAttribute.html)
* [[NativeContainerSupportsDeallocateOnJobCompletion]](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerSupportsDeallocateOnJobCompletionAttribute.html)
* [[NativeDisableUnsafePtrRestriction]](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestrictionAttribute.html)
* [[NativeSetClassTypeToNullOnSchedule]](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeSetClassTypeToNullOnScheduleAttribute.html)

### Other

* \#if ENABLE_UNITY_COLLECTIONS_CHECKS ... #endif

## Burst compiler related

### Attributes

* [[BurstDiscard]](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Burst.BurstDiscardAttribute.html)
* [BurstCompile]