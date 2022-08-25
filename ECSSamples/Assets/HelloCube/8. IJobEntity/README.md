# HelloCube: IJobEntity

## IJobEntity
The `IJobEntity` interface is a feature which replaces `IJobForeach` - The core purpose is to provide an easy way to iterate over entities without the hassle of writing a full `IJobEntityBatch`

## Description

This sample demonstrates how you can leverage the `IJobEntity` interface as well as the `.Schedule` invocation to obviate the need for writing boilerplate code.

Implementing this interface with an `Execute()` method will result in the automatic generation of a corresponding `IJobEntityBatch` type that contains an `Execute()` method performing per-entity iteration, thus saving you the trouble of implementing it yourself.

`IJobEntityBatch` has replaced `IJobChunk`.

## What does it show?

Any type that implements the `IJobEntity` interface _and_ contains an `Execute()` method (with an arbitrary number of `ref` and `in` parameters) will trigger the generation of a corresponding `IJobEntityBatch` type. This generated `IJobEntityBatch` type, inside of its `Execute()` method,  invokes the `Execute()` method of its `IJobEntity
` source with the appropriate arguments.

In this sample, the following `IJobEntityBatch` type is generated as a nested type inside of the `SystemBase` class in which the source `IJobEntity` type is invoked:

```cs
public Unity.Entities.ComponentTypeHandle<Unity.Transforms.Rotation> __RotationTypeHandle;
[Unity.Collections.ReadOnly]
public Unity.Entities.ComponentTypeHandle<RotationSpeed_IJobEntity> __RotationSpeed_IJobEntityTypeHandle;
public void Execute(ArchetypeChunk batch, int batchIndex)
{
    var rotationData = InternalCompilerInterface.UnsafeGetChunkNativeArrayIntPtr<Unity.Transforms.Rotation>(batch, __RotationTypeHandle);
    var rotationSpeed_IJobEntityData = InternalCompilerInterface.UnsafeGetChunkNativeArrayReadOnlyIntPtr<RotationSpeed_IJobEntity>(batch, __RotationSpeed_IJobEntityTypeHandle);
    int count = batch.Count;
    for (int i = 0; i < count; ++i)
    {
        ref var rotationData__ref = ref InternalCompilerInterface.UnsafeGetRefToNativeArrayPtrElement<Unity.Transforms.Rotation>(rotationData, i);
        ref var rotationSpeed_IJobEntityData__ref = ref InternalCompilerInterface.UnsafeGetRefToNativeArrayPtrElement<RotationSpeed_IJobEntity>(rotationSpeed_IJobEntityData, i);
        Execute(ref rotationData__ref, in rotationSpeed_IJobEntityData__ref);
    }
}
```

Invoking `new RotateEntityIJobEntitySystemBase().Schedule()` in any method of a `SystemBase`-derived type triggers the generation of the following in the same class:

- An `EntityQuery` field,
- An `OnCreateForCompiler()` method, which populates the generated `EntityQuery` field,
- A modified `OnUpdate()` method.

In this sample, the `RotateEntityIJobEntitySystemBase` class is modified to the following:

```cs
[System.Runtime.CompilerServices.CompilerGenerated]
public partial class RotateEntityIJobEntitySystemBase : SystemBase
{
    [Unity.Entities.DOTSCompilerPatchedMethod("OnUpdate")]
    // OnUpdate runs on the main thread.
    protected void __OnUpdate_2C361387()
    {
        #line 23 "C:\Users\daniel.andersen\Documents\git\unity\dots\Projects\EntitiesSamples\Assets\HelloCube\8. IJobEntity\RotateEntityJobs.cs"
        Dependency = __ScheduleViaJobEntityBatchExtension_0(new RotateEntitySystemBaseJob{DeltaTime = Time.DeltaTime}, __query_0, Dependency);
    }

    Unity.Entities.EntityQuery __query_0;
    Unity.Entities.ComponentTypeHandle<Unity.Transforms.Rotation> __Unity_Transforms_Rotation_RW_ComponentTypeHandle;
    Unity.Entities.ComponentTypeHandle<RotationSpeed_IJobEntity> __RotationSpeed_IJobEntity_RO_ComponentTypeHandle;
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    Unity.Jobs.JobHandle __ScheduleViaJobEntityBatchExtension_0(RotateEntityJob job, Unity.Entities.EntityQuery entityQuery, Unity.Jobs.JobHandle dependency)
    {
        __Unity_Transforms_Rotation_RW_ComponentTypeHandle.Update(this);
        __RotationSpeed_IJobEntity_RO_ComponentTypeHandle.Update(this);
        job.__RotationTypeHandle = __Unity_Transforms_Rotation_RW_ComponentTypeHandle;
        job.__RotationSpeed_IJobEntityTypeHandle = __RotationSpeed_IJobEntity_RO_ComponentTypeHandle;
        return Unity.Entities.JobEntityBatchExtensions.Schedule(job, entityQuery, dependency);
        ;
    }

    protected override void OnCreateForCompiler()
    {
        base.OnCreateForCompiler();
        __query_0 = GetEntityQuery(new Unity.Entities.EntityQueryDesc{All = new Unity.Entities.ComponentType[]{ComponentType.ReadOnly<RotationSpeed_IJobEntity>(), Unity.Entities.ComponentType.ReadWrite<Unity.Transforms.Rotation>()}, Any = new Unity.Entities.ComponentType[]{}, None = new Unity.Entities.ComponentType[]{}, Options = Unity.Entities.EntityQueryOptions.Default});
        __Unity_Transforms_Rotation_RW_ComponentTypeHandle = GetComponentTypeHandle<Unity.Transforms.Rotation>(false);
        ;
        __RotationSpeed_IJobEntity_RO_ComponentTypeHandle = GetComponentTypeHandle<RotationSpeedSystemBaseIJobEntity>(true);
    }
}
```
