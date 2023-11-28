# The C# Job system

In this page:

- [Unmanaged Collections](#unmanaged-collections)
- [C# Jobs](#c-jobs-and-job-dependencies)
- [Job Dependencies](#c-jobs-and-job-dependencies)
- [Job Safety Checks](#job-safety-checks)
- [Parallel Jobs](#parallel-jobs)

Further reading:

1. [Blog post: Improving Job System Performance part 1](https://blog.unity.com/engine-platform/improving-job-system-performance-2022-2-part-1)
1. [Blog post: Improving Job System Performance part 2](https://blog.unity.com/engine-platform/improving-job-system-performance-2022-2-part-2)

<br/>

# Unmanaged collections

The unmanaged collection types of `Unity.Collections` have a few advantages over normal C# managed collections:

- Unmanaged objects can be used in Burst-compiled code.
- Unmanaged objects can be used in jobs, whereas using managed objects in jobs is not always safe.
- The `Native-` collection types have safety checks to help enforce thread-safety in jobs.
- Unmanaged objects are not garbage collected and so induce no garbage collection overhead.

On the downside, you are responsible for calling `Dispose()` on every unmanaged collection once it's no longer needed. Neglecting to dispose a collection creates a memory leak, and the disposal safety checks will throw an error.

*[See more information about unmanaged collections](./cheatsheet/collections.md).*

## Allocators

When instantiating an unmanaged collection, you must specify an *allocator*. Different allocators organize and track their memory in different ways. Three of the most-commonly used allocators are:

- `Allocator.Persistent`: **The slowest allocator. Used for indefinite lifetime allocations.** You must call `Dispose()` to deallocate a Persistent-allocated collection when you no longer need it.
- `Allocator.Temp`: **The fastest allocator. Used for short-lived allocations.** Each frame, the main thread creates a Temp allocator which is deallocated in its entirety at the end of the frame. Because a Temp allocator gets discarded as a whole, you don't actually need to manually deallocate your Temp allocations, and in fact, calling `Dispose()` on a Temp-allocated collection is a no-op.
- `Allocator.TempJob`: *(discussed [below](#allocations-within-jobs))*

<br/>

# C# Jobs and Job Dependencies

&#x1F579;  *[See example jobs](../Assets/ExampleCode/Jobs.cs).*

The C# Jobs system allows us to schedule work to be executed in a pool of worker threads:

- When a worker thread finishes its current work, the thread will pull a waiting job off the queue and invoke the job's `Execute()` method to run the job.
- A job type is created by defining a struct that implements [`IJob`](https://docs.unity3d.com/ScriptReference/Unity.Jobs.IJob.html) or one of the other job interfaces (`IJobParallelFor`, `IJobEntity`, `IJobChunk`...).
- To put a job instance on the job queue, call the extension method `Schedule()`. Jobs can only be scheduled from the main thread, not from within other jobs.

<br>

## Dependencies

`Schedule()` returns a [`JobHandle`](https://docs.unity3d.com/ScriptReference/Unity.Jobs.JobHandle.html) representing the scheduled job. If a `JobHandle` is passed to `Schedule()`, the new job will *depend* upon the job represented by the handle.

**A worker thread will not pull a job off the job queue until the job's dependencies have all finished execution.** So we can use dependencies to prescribe the execution order amongst the scheduled jobs.

Although `Schedule()` only takes one `JobHandle` argument, we can use [`JobHandle.CombineDependencies()`](https://docs.unity3d.com/ScriptReference/Unity.Jobs.JobHandle.CombineDependencies.html)'s to combine multiple handles into one logical handle, thus allowing a job to have multiple direct dependencies.

<br>

## Completing jobs

At some point after scheduling a job, the main thread should call the `JobHandle`'s [`Complete()`](https://docs.unity3d.com/ScriptReference/Unity.Jobs.JobHandle.Complete.html) method on the main thread. Completing a job does a few things in this order:

1. Recursively completes all dependencies of the job.
1. Waits for the job to finish execution if it hasn't finished already. 
1. Removes all remaining references of the job from the job queue.

Effectively, once `Complete()` returns, the job and all its dependencies are guaranteed to have finished execution and to have been removed from the queue.

Also note:

- Calling `Complete()` on the handle of an already completed job does nothing and throws no error.
- Like with scheduling, jobs can only be completed from the main thread, not from within other jobs. 
- Though a job can be completed immediately after scheduling, it's usually best to hold off completing a job until the latest possible moment when the work actually needs to be done. In general, the longer the gaps between the scheduling of each job and its completion, the less likely the main thread and worker threads will spend time needlessly sitting idle.

<br>

## Data access in jobs

In the large majority of cases:

- A job should not perform I/O.
- A job should not access managed objects.
- A job should only access static fields if they are readonly.

Scheduling a job creates a private copy of the struct that will be visible only to the running job. Consequently, any modifications to the fields in the job will be visible only within the job. However, because an unmanaged collection struct stores its *content* externally instead of in the struct itself, modifications to the content of a collection field will be visible outside the job.

<br>

## Allocations within jobs

Collections passed to a job must be allocated with `Allocator.Persistent`, `Allocator.TempJob`, or another thread-safe allocator.

Collections allocated with `Allocator.Temp` *cannot* be passed into jobs. However, each thread of a job is given its own Temp allocator, so `Allocator.Temp` is safe to use *within* jobs. All Temp allocations in a job will be disposed automatically at the end of the job.

Allocations made with `Allocator.TempJob` must be manually disposed. The disposal safety checks, if enabled, will throw an exception when any allocation made with `Allocator.TempJob` is not disposed within 4 frames after allocation.


<br/>


# Job Safety Checks

For any two jobs which access the same data, it's generally undesirable for their execution to overlap or for their execution order to be indeterminate. For example, if two jobs read and write the content of a native array, we should ensure that one of the two jobs finishes execution before the other starts. Otherwise, when either job modifies the array, that change may interfere with the results of the other job, depending upon the happenstance of which job runs before the other and whether their execution overlaps.

So when you have such a data conflict between two jobs, you should either:

- Schedule and complete one job before scheduling the other...
- ...or schedule one job as a dependency of the other.

When you call `Schedule()`, the job safety checks (if enabled) will throw an exception if they detect a potential race condition. For instance, an exception will be thrown if you first schedule a job that uses a native array and then schedule a second job which uses that same native array but which does not depend upon the first job.

As a special case, it's always safe for two jobs to access the same data if both jobs only *read* the data. Because neither job modifies the data, they won't interfere with each other. We can indicate that a native array or collection will only be read in a job by marking the struct field with the [`[ReadOnly]`](https://docs.unity3d.com/ScriptReference/Unity.Collections.ReadOnlyAttribute.html) attribute. The job safety checks will not consider two jobs to conflict if all native arrays or collections they share are marked `[ReadOnly]` in both jobs.

In some cases, you may wish to disable the job safety checks entirely for a specific native array or collection used in a job. This can be done by marking it with the [`[NativeDisableContainerSafetyRestriction]`](https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeDisableContainerSafetyRestrictionAttribute.html) attribute. Just be sure that you're not creating a race condition!

While a native collection is in use by any currently scheduled jobs, the safety checks will throw an exception if you attempt to read or modify that native collection on the main thread. As a special case, the main thread can *read* from a native collection if it is marked `[ReadOnly]` in the scheduled jobs.

<br/>


# Parallel Jobs

To split the work of processing an array or list across multiple threads, we can define a job with the [`IJobParallelFor`](https://docs.unity3d.com/ScriptReference/Unity.Jobs.IJobParallelFor.html) interface:

```csharp
[BurstCompile]
public struct SquareNumbersJob : IJobParallelFor
{
    public NativeArray<int> Nums;

    // Each Execute call processes only a single index.
    public void Execute(int index)
    {
        Nums[index] *= Nums[index];
    }
}
```

When we schedule the job, we specify an index count and batch size:
 
```csharp
// ... scheduling the job
var job = new SquareNumbersJob { Nums = myArray };
JobHandle handle = job.Schedule(
        myArray.Length,    // count
        100);              // batch size
```

When the job runs, its `Execute()` will be called *`count`* times, with all values from 0 up to *count* passed to `index`.

The indexes of the job get split into batches determined by the batch size, and the worker threads then can grab these batches off the queue. Effectively, the separate batches may be processed concurrently on separate threads, but all indexes of an individual batch will be processed together within a single thread.

In this example, if the array length is, say, 250, then the job will be split into three batches: the first covering indexes 0 through 99; the second covering indexes 100 through 199; and the last batch covering the remainder, indexes 200 through 249. Because the job is split into three batches, it will effectively be processed at most across three worker threads. If we want to split the job up across more threads, we must pick a smaller batch size.

| &#x1F4DD; NOTE |
| :- |
| The choice of a good batch size isn’t an exact science! In the extreme case, we could pick a batch size of 1 and thereby split each individual index into its own batch, but keep in mind that having too many small batches might incur significant job system overhead. In general, you should pick a batch size that seems not too big but not too small and then experiment to find a size that seems optimal for each specific job. |

When a batch is processed, it should only access array or list indexes of its own batch. To enforce this, the safety checks throw an exception if we index an array or list with any value other than the `index` parameter:

```csharp
[BurstCompile]
public struct MyJob : IJobParallelFor
{
    public NativeArray<int> Nums;

    public void Execute(int index)
    {
        // The expression Nums[0] triggers a safety check exception!
        Nums[index] = Nums[0];
    }
}
```

This restriction does not apply to array and list fields marked with the `[ReadOnly]` attribute. For an array or list field you need to write in the job, you can disable the restriction by marking the field with `[NativeDisableParallelForRestriction]`. Just be careful that you're not creating a race condition!