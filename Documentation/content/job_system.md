# C# Job System

## How the C# Job System works

The Unity C# Job System is designed to allow users to write [multithreaded](https://en.wikipedia.org/wiki/Multithreading_(computer_architecture)) code that interacts well with the rest of the Unity engine, and makes it easier to write correct code.

Writing multithreaded code can provide great performance benefits, including significant gains in frame rate, and improved battery life for mobile devices.

An important aspect of the C# Job System is that it integrates with what the engine uses internally (Unity’s native job system). This means that user written code and the engine will share [worker threads](https://msdn.microsoft.com/en-us/library/69644x60.aspx) to avoid creating more threads than [CPU cores](https://en.wikipedia.org/wiki/Multi-core_processor); which would cause contention for CPU resources. 

## What is multithreading?

Multithreading is a type of programming that takes advantage of the fact that a CPU can process multiple threads at the same time. Instead of coded tasks or instructions executing one after another, they execute simultaneously. 

A main thread runs at the start of a program by default. Then the main thread creates new threads (often called worker threads) based on the code. The worker threads then run in [parallel](https://en.wikipedia.org/wiki/Parallel_computing) to one another and usually synchronize their results with the main thread once completed. 

This approach to multithreading works well if you have a few tasks that run for a long time. Game development code usually contains multiple small tasks and instructions to execute at once. If you create a thread for each one, you can end up with many threads, each with a short lifetime. This can push the limits of the processing capacity of your CPU and operating system.

You can mitigate the issue of thread lifetime by having a [pool of threads](https://en.wikipedia.org/wiki/Thread_pool), but even if you do, you will have a large amount of threads alive at the same time. Having more threads than CPU cores leads to the threads contending with each other for CPU resources, with frequent [context switches](https://en.wikipedia.org/wiki/Context_switch) as a result. 

Context switching is the process of saving the state of a thread part way through execution, then working on another thread, and then reconstructing the first thread later on to continue processing it. Context switching is resource-intensive, so it’s important to avoid the need for it wherever possible.

## What is a job system?

A job system manages multithreaded code in a different way. Instead of systems creating threads they create something called [jobs](https://en.wikipedia.org/wiki/Job_(computing)). A job receives parameters and operates on data, similar to how a method call behaves. Jobs should be fairly small, and do one specific task. 

A job system puts jobs into a queue to execute. Worker threads in a job system take items from the job queue and execute them. A job system usually has one worker thread per logical CPU core, to avoid context switching.

If each job is self contained this is all you need. However, this is unlikely in complex systems like those required for game development. So what you usually end up with is one job preparing the data for the next job. To manage this, jobs are aware of and support [dependencies](http://tutorials.jenkov.com/ood/understanding-dependencies.html).

If **job A** has a dependency on **job B**, the job system ensures that **job A** does not start executing until **job B** is complete.

## Race conditions

When writing multithreaded code there is always a risk for [race conditions](https://en.wikipedia.org/wiki/Race_condition). A race condition occurs when the output of one operation depends on the timing of another operation outside of its control. 

A race condition is not always a bug, but it is always a source of indeterministic behaviour. When a race condition does cause a bug, it can be difficult to find the source of the problem because it depends on timing. This means you can only recreate the issue on rare occasions, and debugging it can cause the problem to disappear; because breakpoints and logging can change the timing too. To a large extent, this is what produces the largest challenge in writing multithreaded code.

## Safety system

To make it easier to write multithreaded code in the C# Job System there is a safety system build in. The safety system detects all potential race conditions and protects you from the bugs they can cause.

The main way the C# Job System solves this is to send each job a copy of the data it needs to operate on, rather than a reference to the data in the main thread. This isolates the data, which eliminates the race condition. 

The way the C# Job System copies data means that a job can only access [blittable data types](https://en.wikipedia.org/wiki/Blittable_types) (which do not require conversion when passed between [managed](https://en.wikipedia.org/wiki/Managed_code) and native code).

The C# Job System can copy blittable types with [memcpy](http://www.cplusplus.com/reference/cstring/memcpy/) and transfer the data between the managed and native parts of Unity. It uses `memcpy` to put it into native memory on [Schedule](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Jobs.IJobExtensions.Schedule.html) and gives the managed side access to that copy on [Execute](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Jobs.IJob.Execute.html). This can be limiting, because you cannot return a result from the job. 

## NativeContainers

There is one exception to the rule of copying data. That exception is [NativeContainers](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerAttribute.html).

A `NativeContainer` is a managed [value type](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/value-types) that provides a relatively safe C# wrapper for native memory. When used with the C# Job System, it allows a job to access shared data on the main thread rather than working with a copy of it.

Unity ships with a set of `NativeContainers`: [NativeArray](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.NativeArray_1.html), NativeList, NativeHashMap, and NativeQueue.

> Note: Only `NativeArray` is available without the ECS package.

You can also manipulate `NativeArrays` with [NativeSlice](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.NativeSlice_1.html) to get a subset of the `NativeArray` from a certain position to a certain length.

All `NativeContainers` have the safety system built in, which tracks all `NativeContainers`, and what is reading and writing to them.

For example, if two scheduled jobs are writing to the same `NativeArray`, the safety system throws an exception with a clear error message that explains why and how to solve the problem. In this case, you can always schedule a job with a dependency so that the first job can write to the `NativeContainer`, and once it has finished executing, the next job can safely read and write to that `NativeContainer` as well. 

The same read and write restrictions apply when accessing the data from the main thread. The safety system allows many jobs to read from the same data in parallel.

Some `NativeContainers` also have special rules for allowing safe and deterministic write access from [_ParallelFor jobs_](#parallelfor-jobs). As an example, the method `NativeHashMap.Concurrent` lets you add items in parallel from [IJobParallelFor](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Jobs.IJobParallelFor.html).

> Note: There is no protection against accessing static data from within a job. Accessing static data circumvents all safety systems and can crash Unity. For more information, see [_Troubleshooting_](#job-system-tips-and-troubleshooting).

## NativeContainer Allocators

There are three types of [Allocators](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.Allocator.html) that handle `NativeContainer` memory allocation and release. You will need to specify one of these when instantiating a `NativeContainer`.

* **Allocator.Temp** has the fastest allocation. It is intended for allocations with a lifespan of 1 frame or fewer and is not thread-safe. `Temp` `NativeContainer` allocations should call the `Dispose` method before you return from the function call.
* **Allocator.TempJob** is a slower allocation than `Temp`, but is faster than `Persistent`. It is intended for allocations with a lifespan of 4 frames or fewer and is thread-safe. Most short jobs use this `NativeContainer` allocation type.
* **Allocator.Persistent** is the slowest allocation, but lasts throughout the application lifetime. It is a wrapper for a direct call to [malloc](http://www.cplusplus.com/reference/cstdlib/malloc/). Longer jobs may use this `NativeContainer` allocation type.

For example:

```c#
NativeArray<float> result = new NativeArray<float>(1, Allocator.Temp);
```

## Scheduling jobs

To schedule a job you need to implement the [IJob](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Jobs.IJob.html) interface. This allows you to schedule a single job that runs in parallel to other jobs and the main thread. To do this, create an instance of your struct, populate it with data and call the `Schedule` method. Calling `Schedule` will put the job into the job queue to be executed at the appropriate time. 

When you schedule a job, you will get back a [JobHandle](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Jobs.JobHandle.html) you can use in your code as a dependency for other jobs. Otherwise, you can force your code to wait in the main thread for your job to finish executing by calling the method `Complete` on the `JobHandle`; at which point you know your code can safely access the `NativeContainers` on the main thread again.

> Note: Jobs do not start executing immediately when you schedule them, unless you state that you are waiting for them in the main thread by calling the method `JobHandle.Complete`. This flushes them from the memory cache and starts the process of execution. Without `JobHandle.Complete`, you need to explicitly flush the batch by calling the static `JobHandle.ScheduleBatchedJobs` method.

## Code example: Scheduling a job that adds two floating point numbers together

**Job code**:

```C#
// Job adding two floating point values together
public struct MyJob : IJob
{
    public float a;
    public float b;
    NativeArray<float> result;
    
    public void Execute()
    {
        result[0] = a + b;
    }
}
```
**Main thread code**:

```C#
// Create a native array of a single float to store the result in. This example will wait for the job to complete, which means we can use Allocator.Temp
NativeArray<float> result = new NativeArray<float>(1, Allocator.Temp);

// Setup the job data
MyJob jobData = new MyJob();
jobData.a = 10;
jobData.b = 10;
jobData.result = result;

// Schedule the job
JobHandle handle = jobData.Schedule();

// Wait for the job to complete
handle.Complete();

// All copies of the NativeArray point to the same memory, we can access the result in "our" copy of the NativeArray
float aPlusB = result[0];

// Free the memory allocated by the result array
result.Dispose();
```
## Code example: Many jobs operating on the same data using dependencies

**Job code**:

```C#
public struct AddOneJob : IJob
{
    public NativeArray<float> result;
    
    public void Execute()
    {
        result[0] = result[0] + 1;
    }
}
```
**Main thread code**:

```C#
NativeArray<float> result = new NativeArray<float>(1, Allocator.Temp);

// Setup the job data
MyJob jobData = new MyJob();
jobData.a = 10;
jobData.b = 10;
jobData.result = result;

// Schedule the job
JobHandle firstHandle = jobData.Schedule();
AddOneJob incJobData = new AddOneJob();
incJobData.result = result;
JobHandle handle = incJobData.Schedule(firstHandle);

// Wait for the job to complete
handle.Complete();

// All copies of the NativeArray point to the same memory, we can access the result in "our" copy of the NativeArray
float aPlusB = result[0];

// Free the memory allocated by the result array
result.Dispose();
```

## ParallelFor jobs

When scheduling jobs, there can only be one job doing one task. In a game, it is common to want to perform the same operation on a large number of datapoints. These are called [SIMD](https://en.wikipedia.org/wiki/SIMD) operations. To handle this, there is a separate job type called `IJobParallelFor`.

> Note: A ParallelFor job is a collective term in Unity for any job that implements the `IJobParallelFor` interface.

A ParallelFor job uses a `NativeArray` as its data source and runs across multiple cores. `IJobParallelFor` behaves like `IJob`, but instead of getting a single `Execute` callback, you get one `Execute` callback per item in the `NativeArray`. The system does not actually schedule one job per item, it schedules up to one job per CPU core and redistributes the workload. The system deals with this internally.

When scheduling ParallelForJobs you must specify the length of the `NativeArray` you are splitting, since the system cannot know which `NativeArray` you want to use as primary if there are several in the struct. You also need to specify a batch count. The batch count controls how many jobs you get, and how fine-grained the redistribution of work between threads is.

Having a low batch count, such as 1, gives you a more even distribution of work between threads. It does come with some overhead, so sometimes it is better to increase the batch count. Starting at 1 and increasing the batch count until there are negligible performance gains is a valid strategy.

## Code example: Scheduling a ParallelFor job that adds two floating point numbers together

**Job code**:

```C#
// Job adding two floating point values together
public struct MyParallelJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float> a;
    [ReadOnly]
    public NativeArray<float> b;
    public NativeArray<float> result;
    
    public void Execute(int i)
    {
        result[i] = a[i] + b[i];
    }
}
```
**Main thread code**:

```C#
var jobData = new MyParallelJob();
jobData.a = new NativeArray<float>(new float[] { 1, 2, 3 }, Allocator.TempJob);
jobData.b = new NativeArray<float>(new float[] { 6, 7, 8 }, Allocator.TempJob);
jobData.result = new NativeArray<float>(3, Allocator.TempJob);

// Schedule the job with one Execute per index in the results array and only 1 item per processing batch
JobHandle handle = jobData.Schedule(jobData.result.Length, 1);

// Wait for the job to complete
handle.Complete();

jobData.a.Dispose();
jobData.b.Dispose();
jobData.result.Dispose();
```

## Job System tips and troubleshooting

When using the C# job system, make sure you adhere to the following:

### Do not access static data from a job

Accessing static data from a job circumvents all safety systems. If you access the wrong data, you might crash Unity, often in unexpected ways. (Accessing [MonoBehaviour](https://docs.unity3d.com/ScriptReference/MonoBehaviour.html) can, for example, cause crashes on domain reloads). Because of this risk, future versions of Unity will prevent global variable access from jobs using static analysis, so note that if you do access static data inside a job, you should expect your code to break in future versions of Unity.

### Always flush schedule batches

When you want your jobs to start, you need to flush the scheduled batch with `JobHandle.ScheduleBatchedJobs`. Not doing this delays the scheduling until another job waits for the result.

### Don’t try to update NativeArray contents

Due to the lack of [ref returns](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/ref-returns), it is not possible to directly change the content of a `NativeArray`. `nativeArray[0]++;` is the same as writing `var temp = nativeArray[0]; temp++;` which does not update the value in the `nativeArray`. (Unity is working on C# 7.0 support, which will add ref returns and solve this.)

### Always call `JobHandle.Complete`

Tracing data ownership requires dependencies to complete before the main thread can use them again. This means that it is not enough to check `JobHandle.IsDone`. You must call the method `JobHandle.Complete` to regain ownership of the `NativeContainers` to the main thread. Calling `Complete` also cleans up the state in the jobs debugger. Not doing so introduces a memory leak. This also applies if you schedule new jobs every frame that have a dependency on the previous frame's job.
