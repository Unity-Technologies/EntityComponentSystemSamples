# Custom job types

On the lowest level of the job system, jobs are scheduled by calling one of the `Schedule` functions in [JobsUtility](https://docs.unity3d.com/ScriptReference/Unity.Jobs.LowLevel.Unsafe.JobsUtility.html). The currently existing [job types](https://docs.unity3d.com/ScriptReference/Unity.Jobs.LowLevel.Unsafe.JobType.html) all use these functions, but it is also possible to create specialized job types using the same APIs.

These APIs use unsafe code and have to be crafted carefully, since they can easily introduce unwanted race conditions. If you add your own job types, we strongly recommend to aim for full test coverage.

As an example we have a custom job type `IJobParallelForBatch` (see file: _/Packages/com.unity.jobs/Unity.Jobs/IJobParallelForBatch.cs_).

It works like __IJobParallelFor__, but instead of calling a single execute function per index it calls one execute function per batch being executed. This is useful if you need to do something on more than one item at a time, but still want to do it in parallel. A common scenario for this job type is if you need to create a temporary array and you want to avoid creating each item in the array one at a time. By using IJobParallelFor you can instead create one temporary array per batch.

In the IJobParallelForBatch example, the entry point where the job is actually scheduled looks like this:
```C#
unsafe static public JobHandle ScheduleBatch<T>(this T jobData, int arrayLength, int minIndicesPerJobCount, JobHandle dependsOn = new JobHandle()) where T : struct, IJobParallelForBatch
{
    var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), ParallelForBatchJobStruct<T>.Initialize(), dependsOn, ScheduleMode.Batched);
    return JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, minIndicesPerJobCount);
}
```
The first line creates a struct containing the scheduling parameters. When creating it you need to set a pointer to the data which will be copied to the jobs. The reason this is a pointer is that the native code which uses it does not know about the type.
You also need to pass it a pointer to the __JobReflectionData__ created by calling:
```C#
JobsUtility.CreateJobReflectionData(typeof(T), JobType.ParallelFor, (ExecuteJobFunction)Execute);
```
JobReflection stores information about the struct with the data for the job, such as which __NativeContainers__ it has and how they need to be patched when scheduling a job. It lives on the native side of the engine and the managed code only has access to it though pointers without any information about what the type is. When creating JobReflectionData you need to specify the type of the struct implementing the job, the __JobType__ and the method which will be called to execute the job. The JobReflectionData does not depend on the data in the struct you schedule, only its type, so it should only be created once for all jobs implementing the same interface. There are currently only two job types, __Single__ and __ParallelFor__. Single means the job will only get a single call, ParallelFor means there will be multiple calls to process it; where each call is restricted to a subset of the range of indices to process. Which job type you choose affects which schedule function you are allowed to call.

The third parameter of __JobsUtility.JobScheduleParameters__ is the __JobHandle__ that the scheduled job should depend on.

The final parameter is the schedule mode. There are two scheduling modes to choose from, __Run__ and __Batched__. Batched means one or more jobs will be scheduled to do the processing, while Run means the processing will be done on the main thread before Schedule returns.

Once the schedule parameters are created we actually schedule the job. There are three ways to schedule jobs depending on their type:
```C#
JobHandle Schedule(ref JobScheduleParameters parameters);
JobHandle ScheduleParallelFor(ref JobScheduleParameters parameters, int arrayLength, int innerLoopBatchCount);
JobHandle ScheduleParallelForTransform(ref JobScheduleParameters parameters, IntPtr transfromAccessArray);
```
Schedule can only be used if the __ScheduleParameters__ are created with __JobType.Single__, the other two schedule functions require __JobType.ParallelFor__.
The __arrayLength__ and __innerLoopBatchCount__ parameter passed to __ScheduleParallelFor__ are used to determine how many indices the jobs should process and how many indices it should handle in the inner loop (see the section on [Execution and JobRanges](#execution-and-jobranges) for more information on the inner loop count).
__ScheduleParallelForTransform__ is similar to ScheduleParallelFor, but it also has access to a __TransformAccessArray__ that allows you to modify __Transform__ components on __GameObjects__. The number of indices and batch size is inferred from the TransformAccessArray.

## Execution and JobRanges

After scheduling the job, Unity will call the entry point you specified directly from the native side. It works in a similar way to how __Update__ is called on MonoBehaviours, but from inside a job instead. You only get one call per job and there is either one job, or one job per worker thread; in the case of ParallelFor.

The signature used for Execute is
```C#
public delegate void ExecuteJobFunction(ref T data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
```
For Single jobs, only the data is needed and you can just do your processing right away, but for ParallelFor jobs it requires some more work before you can start processing indices. We need to split up the indices into a number of sequential sub-sets that each job will process in parallel. This way we do not process the same thing twice and we are sure that everything gets covered. The memory layout will determine the order of indices.  

The JobRanges contain the batches and indices a ParallelFor job is supposed to process. The indices are split into batches based on the batch size, the batches are evenly distributed between the jobs doing the execution in such a way that each job can iterate over a continuous section of memory. The ParallelFor job should call:
```C#
JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end)
```
This continues until it returns `false`, and after calling it process all items with index between __begin__ and __end__.
The reason you get batches of items, rather than the full set of items the job should process, is that Unity will apply [work stealing](https://en.wikipedia.org/wiki/Work_stealing) if one job completes before the others. Work stealing in this context means that when one job is done it will look at the other jobs running and see if any of them still have a lot of work left. If it finds a job which is not complete it will steal some of the batches that it has not yet started; to dynamically redistribute the work.

Before a ParallelFor job starts processing items it also needs to limit the write access to NativeContainers on the range of items which the job is processing. If it does not do this several jobs can potentially write to the same index which leads to race conditions. The NativeContainers that need to be limited is passed to the job and there is a function to patch them; so they cannot access items outside the correct range. The code to do it looks like this:
```C#
#if ENABLE_UNITY_COLLECTIONS_CHECKS
JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), begin, end - begin);
#endif
```

# Custom NativeContainers

When writing jobs, the data communication between jobs is one of the hardest parts to get right. Just using __NativeArray__ is very limiting. Using __NativeQueue__, __NativeHashMap__ and __NativeMultiHashMap__ and their __Concurrent__ versions solves most scenarios.

For the remaining scenarios it is possible to write your own custom NativeContainers.
When writing custom containers for [thread synchronization](https://en.wikipedia.org/wiki/Synchronization_(computer_science)#Thread_or_process_synchronization) it is very important to write correct code. We strongly recommend full test coverage for any new containers you add.

As a very simple example of this we will create a __NativeCounter__ that can be incremented in a ParallelFor job through __NativeCounter.Concurrent__ and read in a later job or on the main thread.

Let's start with the basic container type:
```C#
// Mark this struct as a NativeContainer, usually this would be a generic struct for containers, but a counter does not need to be generic
// TODO - why does a counter not need to be generic? - explain the argument for this reasoning please.
[StructLayout(LayoutKind.Sequential)]
[NativeContainer]
unsafe public struct NativeCounter
{
    // The actual pointer to the allocated count needs to have restrictions relaxed so jobs can be schedled with this container
    [NativeDisableUnsafePtrRestriction]
    int* m_Counter;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
    AtomicSafetyHandle m_Safety;
    // The dispose sentinel tracks memory leaks. It is a managed type so it is cleared to null when scheduling a job
    // The job cannot dispose the container, and no one else can dispose it until the job has run, so it is ok to not pass it along
    // This attribute is required, without it this NativeContainer cannot be passed to a job; since that would give the job access to a managed object
    [NativeSetClassTypeToNullOnSchedule]
    DisposeSentinel m_DisposeSentinel;
#endif

    // Keep track of where the memory for this was allocated
    Allocator m_AllocatorLabel;

    public NativeCounter(Allocator label)
    {
        // This check is redundant since we always use an int that is blittable.
        // It is here as an example of how to check for type correctness for generic types.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        if (!UnsafeUtility.IsBlittable<int>())
            throw new ArgumentException(string.Format("{0} used in NativeQueue<{0}> must be blittable", typeof(int)));
#endif
        m_AllocatorLabel = label;

        // Allocate native memory for a single integer
        m_Counter = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>(), 4, label);

        // Create a dispose sentinel to track memory leaks. This also creates the AtomicSafetyHandle
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0);
#endif
        // Initialize the count to 0 to avoid uninitialized data
        Count = 0;
    }

    public void Increment()
    {
        // Verify that the caller has write permission on this data. 
        // This is the race condition protection, without these checks the AtomicSafetyHandle is useless
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        (*m_Counter)++;
    }

    public int Count
    {
        get
        {
            // Verify that the caller has read permission on this data. 
            // This is the race condition protection, without these checks the AtomicSafetyHandle is useless
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return *m_Counter;
        }
        set
        {
            // Verify that the caller has write permission on this data. This is the race condition protection, without these checks the AtomicSafetyHandle is useless
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            *m_Counter = value;
        }
    }

    public bool IsCreated
    {
        get { return m_Counter != null; }
    }

    public void Dispose()
    {
        // Let the dispose sentinel know that the data has been freed so it does not report any memory leaks
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        DisposeSentinel.Dispose(m_Safety, ref m_DisposeSentinel);
#endif

        UnsafeUtility.Free(m_Counter, m_AllocatorLabel);
        m_Counter = null;
    }
}
```
With this we have a simple NativeContainer where we can get, set, and increment the count. This container can be passed to a job, but it has the same restrictions as NativeArray, which means it cannot be passed to a ParallelFor job with write access.

The next step is to make it usable in ParallelFor. In order to avoid race conditions we want to make sure no-one else is reading it while the ParallelFor is writing to it. To achieve this we create a separate inner struct called Concurrent that can handle multiple writers, but no readers. We make sure NativeCounter.Concurrent can be assigned to from within a normal NativeCounter, since it is not possible for it to live separately outside a NativeCounter. TODO - why is that?
```C#
[NativeContainer]
// This attribute is what makes it possible to use NativeCounter.Concurrent in a ParallelFor job
[NativeContainerIsAtomicWriteOnly]
unsafe public struct Concurrent
{
    // Copy of the pointer from the full NativeCounter
    [NativeDisableUnsafePtrRestriction]
    int* 	m_Counter;

    // Copy of the AtomicSafetyHandle from the full NativeCounter. The dispose sentinel is not copied since this inner struct does not own the memory and is not responsible for freeing it.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
    AtomicSafetyHandle m_Safety;
#endif

    // This is what makes it possible to assign to NativeCounter.Concurrent from NativeCounter
    public static implicit operator NativeCounter.Concurrent (NativeCounter cnt)
    {
        NativeCounter.Concurrent concurrent;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckWriteAndThrow(cnt.m_Safety);
        concurrent.m_Safety = cnt.m_Safety;
        AtomicSafetyHandle.UseSecondaryVersion(ref concurrent.m_Safety);
#endif

        concurrent.m_Counter = cnt.m_Counter;
        return concurrent;
    }

    public void Increment()
    {
        // Increment still needs to check for write permissions
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        // The actual increment is implemented with an atomic, since it can be incremented by multiple threads at the same time
        Interlocked.Increment(ref *m_Counter);
    }
}
```

With this setup we can schedule ParallelFor with write access to a NativeCounter through the inner Concurrent struct, like this:
```C#
struct CountZeros : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<int> input;
    public NativeCounter.Concurrent counter;
    public void Execute(int i)
    {
        if (input[i] == 0)
        {
            counter.Increment();
        }
    }
}
```
```C#
var counter = new NativeCounter(Allocator.Temp);
var jobData = new CountZeros();
jobData.input = input;
jobData.counter = counter;
counter.Count = 0;

var handle = jobData.Schedule(input.Length, 8);
handle.Complete();

Debug.Log("The array countains " + counter.Count + " zeros");
counter.Dispose();
```

## Better cache usage

The NativeCounter from the previous section is a working implementation of a counter, but all jobs in the ParallelFor will access the same atomic to increment the value. This is not optimal as it means the same cache line is used by all threads.
The way this is generally solved in NativeContainers is to have a local cache per worker thread, which is stored on its own cache line.

The __[NativeSetThreadIndex]__ attribute can inject a worker thread index, the index is guaranteed to be unique while accessing the NativeContainer from the ParallelFor jobs.

In order to make such an optimization here we need to change a few things. The first thing we need to change is the data layout. For performance reasons we need one full cache line per worker thread, rather than a single int to avoid [false sharing](https://en.wikipedia.org/wiki/False_sharing). 

We start by adding a constant for the number of ints on a cache line.
```C#
public const int IntsPerCacheLine = JobsUtility.CacheLineSize / sizeof(int);
```
Next we change the amount of memory allocated.
```C#
// One full cache line (integers per cacheline * size of integer) for each potential worker index, JobsUtility.MaxJobThreadCount
m_Counter = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>()*IntsPerCacheLine*JobsUtility.MaxJobThreadCount, 4, label);
```

TODO: I'm not sure which example you are referring to when you say: main, non-concurrent, version below (is this an example you used on this page or what you would do if you were not using jobified code/ECS etc. It has potential for confusion.)

When accessing the counter from the main, non-concurrent, version there can only be one writer so the increment function is fine with the new memory layout.
For `get` and `set` of the `count` we need to loop over all potential worker indices.
```C#
public int Count
{
    get
    {
        // Verify that the caller has read permission on this data. 
        // This is the race condition protection, without these checks the AtomicSafetyHandle is useless
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
        int count = 0;
        for (int i = 0; i < JobsUtility.MaxJobThreadCount; ++i)
            count += m_Counter[IntsPerCacheLine * i];
        return count;
    }
    set
    {
        // Verify that the caller has write permission on this data. 
        // This is the race condition protection, without these checks the AtomicSafetyHandle is useless
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        // Clear all locally cached counts, 
        // set the first one to the required value
        for (int i = 1; i < JobsUtility.MaxJobThreadCount; ++i)
            m_Counter[IntsPerCacheLine * i] = 0;
        *m_Counter = value;
    }
}
```

The final change is the inner Concurrent struct that needs to get the worker index injected into it. Since each worker only runs one job at a time, there is no longer any need to use atomics when only accessing the local count.
```C#
[NativeContainer]
[NativeContainerIsAtomicWriteOnly]
// Let the job system know that it should inject the current worker index into this container
unsafe public struct Concurrent
{
    [NativeDisableUnsafePtrRestriction]
    int* 	m_Counter;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
    AtomicSafetyHandle m_Safety;
#endif

    // The current worker thread index; it must use this exact name since it is injected
    [NativeSetThreadIndex]
    int m_ThreadIndex;

    public static implicit operator NativeCacheCounter.Concurrent (NativeCacheCounter cnt)
    {
        NativeCacheCounter.Concurrent concurrent;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckWriteAndThrow(cnt.m_Safety);
        concurrent.m_Safety = cnt.m_Safety;
        AtomicSafetyHandle.UseSecondaryVersion(ref concurrent.m_Safety);
#endif

        concurrent.m_Counter = cnt.m_Counter;
        concurrent.m_ThreadIndex = 0;
        return concurrent;
    }

    public void Increment()
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        // No need for atomics any more since we are just incrementing the local count
        ++m_Counter[IntsPerCacheLine*m_ThreadIndex];
    }
}
```
Writing the NativeCounter this way significantly reduces the overhead of having multiple threads writing to it. It does, however, come at a price. The cost of getting the count on the main thread has increased significantly since it now needs to check all local caches and sum them up. If you are aware of this and make sure to cache the return values it is usually worth it, but you need to know the limitations of your data structures. So we strongly recommend documenting the performance characteristics.

## Tests

The NativeCounter is not complete, the only thing left is to add tests for it to make sure it is correct and that it does not break in the future. When writing tests you should try to cover as many unusual scenarios as possible. It is also a good idea to add some kind of stress test using jobs to detect race conditions, even if it is unlikely to find all of them. The NativeCounter API is very small so the number of tests required is not huge.

* Both versions of the counter examples above are available at: _/Assets/NativeCounterDemo_.
* The tests for them can be found at: _/Assets/NativeCounterDemo/Editor/NativeCounterTests.cs_.

## Available attributes

The NativeCounter uses many attributes, but there are a few more available for other types of containers. Here is a list of the available attributes you can use on the NativeContainer struct.
* [NativeContainer](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerAttribute.html) - marks a struct as a NativeContainer.Required for all native containers.
* [NativeContainerSupportsMinMaxWriteRestriction](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerSupportsMinMaxWriteRestrictionAttribute.html) - signals that the NativeContainer can restrict its writable ranges to be between a min and max index. This is used when passing the container to an IJobParallelFor to make sure that the job does not write to indices it is not supposed to process. In order to use this the NativeContainer must have the members int __m_Length__, int __m_MinIndex__ and int __m_MaxIndex__ in that order with no other members between them. The container must also throw an exception for writes outside the min/max range.
* [NativeContainerIsAtomicWriteOnly](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerIsAtomicWriteOnlyAttribute.html) - signals that the NativeContainer uses atomic writes and no reads. By adding this is is possible to pass the NativeContainer to an IJobParallelFor as writable without restrictions on which indices can be written to.
* [NativeContainerSupportsDeallocateOnJobCompletion](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerSupportsDeallocateOnJobCompletionAttribute.html) - makes the NativeContainer usable with [DeallocateOnJobCompletion](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.DeallocateOnJobCompletionAttribute.html). In order to use this the NativeContainer must have a single allocation in __m_Buffer__, an allocator label in __m_AllocatorLabel__ and a dispose sentinel in __m_DisposeSentinel__.
* [NativeSetThreadIndex](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeSetThreadIndexAttribute.html) - Patches an int with the thread index of the job.

In addition to these attributes on the native container struct itself there are a few attributes which can be used on members of the native container.
* [NativeDisableUnsafePtrRestriction](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestrictionAttribute.html) - allows the NativeContainer to be passed to a job even though it contains a pointer, which is usually not allowed.
* [NativeSetClassTypeToNullOnSchedule](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeSetClassTypeToNullOnScheduleAttribute.html) - allows the NativeContainer to be passed to a job even though it contains a managed object. The managed object will be set to `null` on the copy passed to the job.

[Back to Unity Data-Oriented reference](reference.md)