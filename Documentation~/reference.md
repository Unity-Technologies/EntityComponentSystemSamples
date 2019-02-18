# Unity Data-Oriented reference

Here is a quick reference of the features and concepts that have been introduced in this documentation by ECS, the C# Job System, and Burst. There is also a list of general computing terms that are useful to know when learning the Unity Data-Oriented Tech Stack. 

> **Note**: This is not an exhaustive list and can be added to over time as the Unity Data-Oriented Tech Stack, and its related documentation, expands. Some documentation is located in the [Unity Manual](https://docs.unity3d.com/Manual/index.html) or [Script Reference](https://docs.unity3d.com/ScriptReference/index.html), as some Unity Data-Oriented features are part of the Unity Engine and some features are available in related packages. Some definitions below are not completed or are a "work in progress" marked with WIP. 

## Table of contents

* [Unity Data-Oriented terms](#unity-data-oriented-terms)
* [General computing terms](#general-computing-terms)
* [Further information](#further-information)

## Unity Data-Oriented terms

* Archetype
* Allocator: [Reference](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html)|[Manual](https://docs.unity3d.com/Manual/JobSystemNativeContainer.html) - see "NativeContainer Allocator".
* AtomicSafetyHandle: [Reference](https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.AtomicSafetyHandle.html)|[Manual](https://docs.unity3d.com/Manual/JobSystemNativeContainer.html) - see "NativeContainer and the safety system".
* [Barrier](entity_command_buffer.md#barrier)
* Batch: [Manual](https://docs.unity3d.com/Manual/JobSystemParallelForJobs.html) - see "Scheduling ParallelFor jobs".
* [Burst compiler](burst_compiler.md)
* Burst inspector
* C# Job System: [Manual](https://docs.unity3d.com/Manual/JobSystem.html)
* [Chunk iteration](chunk_iteration.md)
* Component
* [ComponentData](component_data.md)
* ComponentDataArray
* [ComponentDataFromEntity](component_data_from_entity.md)
* ComponentDataProxy
* [ComponentGroup](component_group.md)
* [ComponentSystem](component_system.md)
* ComponentType
* [Custom JobTypes](custom_job_types.md)
* Custom NativeContainers
* DisposeSentinel: [Reference](https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.DisposeSentinel.html)|[Manual](https://docs.unity3d.com/Manual/JobSystemNativeContainer.html) - see "NativeContainer and the safety system".
* [Dynamic Buffers](dynamic_buffers.md)
* [ECS](ecs.md)
* [Entity](entity.md)
* [EntityArchetype](entity_archetype.md)
* [EntityCommandBuffer](entity_command_buffer.md)
* EntityData (?)
* EntityDataManager (?)
* EntityDebugger
* [EntityManager](entity_manager.md)
* [ExclusiveEntityTransaction](exclusive_entity_transaction.md)
* [GameObjectEntity](game_object_entity.md)
* [IComponentData](component_data.md#icomponentdata)
* IJob: [Reference](https://docs.unity3d.com/ScriptReference/Unity.Jobs.IJob.html)|[Manual](https://docs.unity3d.com/Manual/JobSystemCreatingJobs.html)
* IJobParallelFor: [Reference](https://docs.unity3d.com/ScriptReference/Unity.Jobs.IJobParallelFor.html)|[Manual](https://docs.unity3d.com/Manual/JobSystemParallelForJobs.html)
* [IJobParallelForBatch](custom_job_types.md) - see introduction.
* IJobParallelForTransform: [Reference](https://docs.unity3d.com/ScriptReference/Jobs.IJobParallelForTransform.html)|[Manual](https://docs.unity3d.com/Manual/JobSystemParallelForTransformJobs.html)
* IJobProcessComponentData
* [Injection](injection.md)
* [innerloopBatchCount](inner_loop_batch_count.md) WIP
* ISharedComponentData
* Job: [Manual](https://docs.unity3d.com/Manual/JobSystemJobSystems.html) - see "What is a job?".
* [JobComponentSystem](job_component_system.md)
* Job debugger
* JobHandle: [Reference](https://docs.unity3d.com/ScriptReference/Unity.Jobs.JobHandle.html)|[Manual](https://docs.unity3d.com/Manual/JobSystemJobDependencies.html)
* Job queue: [Manual](https://docs.unity3d.com/Manual/JobSystemJobSystems.html) - see "What is a job system?"
* MeshInstanceRenderer
* NativeArray: [Reference](https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeArray_1.html)|[Manual](https://docs.unity3d.com/Manual/JobSystemNativeContainer.html) - see "What types of NativeContainer are available?".
* NativeContainer: [Reference](https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerAttribute.html)|[Manual](https://docs.unity3d.com/Manual/JobSystemNativeContainer.html)
* [NativeHashMap](native_hashmap.md) WIP
* [NativeList](native_list.md) WIP
* [NativeQueue](native_queue.md) WIP
* NativeSlice: [Reference](https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeSlice_1.html)|[Manual](https://docs.unity3d.com/Manual/JobSystemNativeContainer.html) - see "What types of NativeContainer are available?".
* [Safety system](https://docs.unity3d.com/Manual/JobSystemSafetySystem.html) (in the [C# Job System](https://docs.unity3d.com/Manual/JobSystem))
* SharedComponent
* [SharedComponentData](shared_component_data.md)
* SubtractiveComponent
* [SystemStateComponentData](system_state_components.md)
* [SystemStateSharedComponentData](system_state_components.md)
* [System update order](system_update_order.md)
* [TransformSystem](transform_system.md)
* [World](world.md)

## General computing terms

* [AOT compilation](aot_compilation.md)
* [Atomic operation](atomic_operation.md)
* [Blittable types](blittable_types.md)
* Cache lines
* [Context switching](https://docs.unity3d.com/Manual/JobSystemMultithreading.html) - see the end of the page.
* [Dependency](dependency.md)
* [JIT compilation](jit_compilation.md)
* [Job system](https://docs.unity3d.com/Manual/JobSystemJobSystems.html)
* [Logical CPU](logical_cpu.md)
* [Main thread](main_thread.md)
* [Managed code](managed_code.md)
* Memory layout (linear memory layout vs. continuous memory?)
* [Memory leak](memory_leak.md)
* [Multicore](multicore.md)
* [Multithreading](https://docs.unity3d.com/Manual/JobSystemMultithreading.html) 
* Native code
* [Native memory](native_memory.md)
* Parallel computing
* [Performant](performant.md)
* [Race condition](https://docs.unity3d.com/Manual/JobSystemSafetySystem.html)
* [SIMD](simd.md)
* [Unmanaged code](unmanaged_code.md)
* [Worker threads](worker_threads.md)

## Further information

Check the [Unity Data-Oriented manual](manual.md) for more examples on usage, or the [Unity Data-Oriented cheat sheet](cheatsheet.md) for quick links to the code. 