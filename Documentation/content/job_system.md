# Job system

TODO: WHY MULTITHREAD?

## How the job system works

The job system in Unity is designed to allow users to write multi-threaded code which interacts well with the rest of the engine, while making it easier to write correct code.

## What is multithreading?

When most people think of multi-threading they think of creating threads which runs code and then synchronizes its results with the main thread somehow. This works well if you have a few tasks which run for a long time. 

When you are [parallelizing](https://en.wikipedia.org/wiki/Parallel_computing) a game that is rarely the case. Usually you have a huge amount of very small things to do. If you create threads for all of them you will end up with a huge amount of threads with a short lifetime. 

You can mitigate the issue of thread lifetime by having a [pool of threads](https://en.wikipedia.org/wiki/Thread_pool), but even if you do you will have a large amount of threads alive at the same time. Having more threads than CPU cores leads to the threads contending with each other for CPU resources, with frequent [context switches](https://en.wikipedia.org/wiki/Context_switch) as a result. 

When the CPU does a context switch it needs to do a lot of work to make sure the state is correct for the new thread, which can be quite resource intensive and should be avoided if possible.

## What is a job system?

A job system solves the task of parallelizing the code in a slightly different way. Instead of systems creating threads they create something called [jobs](https://en.wikipedia.org/wiki/Job_(computing)). A job is similar to a function call, including the parameters and data it needs, all of which is put into a [job queue](https://en.wikipedia.org/wiki/Job_queue) to execute. Jobs should be kept fairly small and do one specific thing.

The job system has a set of worker threads, usually one thread per logical CPU core to avoid context switching. The worker threads take items from the job queue and executes them.

If each job is self contained this is all you need. However in more complex systems the likelyhood of all jobs being completely self contained is usually not the case, since that would result in large jobs doing a lot of things. So what you usually end up with is one job preparing the data for the next job.

To make this easier, jobs support [dependencies](http://tutorials.jenkov.com/ood/understanding-dependencies.html). If Job A is scheduled with a dependency on Job B the system will guarantee that Job B has completed before Job A starts executing.

An important aspect of the C# job system, and one of the reasons it is a custom API and not one of the existing thread models from C#, is that the job system integrates with what the Unity engine uses internally. This means that user written code and the engine will share worker threads to avoid creating more threads than CPU cores - which would cause contention for CPU resources.

## Race conditions & safety system

When writing multi threaded code there is always a risk for [race conditions](https://en.wikipedia.org/wiki/Race_condition). A race condition means that the output of some operation depends on the timing of some other operation that it cannot control. Whenever someone is writing data, and someone else is reading that data at the same time, there is a race condition. What value the reader sees depends on if the writer executed before or after the reader, which the reader has no control over.

A race condition is not always a bug, but it is always a source of indeterministic behaviour, and when it does lead to bugs such as crashes, deadlocks, or incorrect output it can be difficult to find the source of the problem since it depends on timing. This means the issue can only be recreated on rare occasions and debugging it can cause the problem to disappear; as breakpoints and logging change the timing too. 

To a large extent, this is what makes writing multithreaded code difficult, but fear not - we've got your back.

To make it easier to write multithreaded code the job system in Unity aims to detect all potential race condition and protect you from the bugs they can cause.

The main way this is achieved is by making sure jobs only operate on a copy of all data that is passed to it. If no-one else has access to the data that the job operates on then it cannot possibly cause a race condition. Copying data this way means that a job can only have access to [blittable](https://en.wikipedia.org/wiki/Blittable_types) data, not [managed](https://en.wikipedia.org/wiki/Managed_code) types. This is quite limiting, as you cannot return any result from the job. 

To make it possible to write code to solve real world scenarios there is one exception to the rule of copying data. That exception is __NativeContainers__.

Unity ships with a set of NativeContainers: [__NativeArray__](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Collections.NativeArray_1.html), __NativeList__, __NativeHashMap__, and __NativeQueue__.

All native containers are instrumented with the safety system. Unity tracks all containers and who is reading and writing to it.

For example, if two jobs writing to the same native array are scheduled, the safety system throws an exception with a clear error message explaining why and how to solve the problem. In this case you can always schedule a job with a dependency, so that the first job can write to the container and once it has executed the next job can read & write to that container safely.

Having multiple jobs reading the same data in parallel is allowed of course. 

The same read and write restrictions apply to accessing the data from the main thread.

Some containers also have special rules for allowing safe and deterministic write access from __ParallelFor__ jobs. As an example __NativeHashMap.Concurrent__ lets you add items in parallel from __IJobParallelFor__.

> Note: At the moment protection against accessing static data from within a job is not in place. This means you can technically get access to anything from within the job. This is something we aim to protect against in the future. If you do access static data inside a job you should expect your code to break in future versions.

## Scheduling jobs

As mentioned in the previous section, the job system relies on blittable data and NativeContainers. To schedule a job you need to implement the [__IJob__](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Jobs.IJob.html) interface, create an instance of your struct, fill it with data and call __Schedule__ on it. When you schedule it you will get back a job handle which can be used as a dependency for other jobs, or you can wait for it when you need to access the NativeContainers passed to the job on the main thread again.

Jobs will actually not start executing immediately when you schedule them. We create a batch of jobs to schedule which needs to be flushed. In ECS the batch is implicitly flushed, outside ECS you need to explicitly flush it by calling the static function __JobHandle.ScheduleBatchedJobs()__.
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
If we have multiple jobs operating on the same data we need to use dependencies:
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

Scheduling jobs, as in the previous section, means there can only be one job doing one thing. In a game it very common to want to perform the same operation on a large number of things. For this scenario there is a separate job type: [IJobParallelFor](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Jobs.IJobParallelFor.html).
IJobParallelFor behaves similarly to IJob, but instead of getting a single __Execute__ callback you get one Execute callback per item in an array. The system will not actually schedule one job per item, it will schedule up to one job per CPU core and redistribute the work load, but that is dealt with internally in the system.
When scheduling ParallelForJobs you must specify the length of the array you are splitting, since the system cannot know which array you want to use as primary if there are several in the struct. You also need to specify a batch count. The batch count controls how many jobs you will get, and how fine grained the redistribution of work between threads is.
Having a low batch count, such as 1, will give you a more even distribution of work between threads. It does however come with some overhead so in some cases it is better to increase the batch count slightly. Starting at 1 and increasing the batch count until there are negligible performance gains is a valid strategy.
```C#
// Job adding two floating point values together
public struct MyParallelJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float> a;
    [Readonly]
    public NativeArray<float> b;
    public NativeArray<float> result;
    public void Execute(int i)
    {
        result[i] = a[i] + b[i];
    }
}
```
```C#
var jobData = new MyParallelJob();
jobData.a = 10;  
jobData.b = 10;
jobData.result = result;
// Schedule the job with one Execute per index in the results array and only 1 item per processing batch
JobHandle handle = jobData.Schedule(result.Length, 1);
// Wait for the job to complete
handle.Complete();
```

## Common mistakes

This is a collection of common mistakes when using the job system:

* **Accessing static data from a job**: By doing this you are circumventing all safety systems. If you access the wrong thing you **will** crash Unity, often in unexpected ways. Accessing __MonoBehaviour__ can for example cause crashes on domain reloads. (Future versions will prevent global variable access from jobs using static analysis.)
* **Not flushing schedule batches**: When you want your jobs to start you need to flush the schedule batch with `JobHandle.ScheduleBatchedJobs()`. Not doing so will delay the scheduling until someone waits for the result.
* **Expecting [ref returns](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/ref-returns)**: Due to the lack of ref returns it is not possible to directly modify the content of a NativeArray. ```nativeArray[0]++;``` is the same as writing ```var temp = nativeArray[0]; temp++;``` which will not update the value in the NativeArray. (We are working on C#7 support which will add ref returns and solve this.)
* **Not calling JobHandle.Complete**: The tracing of ownership of data requires that dependencies are completed before the main thread can use them again. This means that it is not enough to just check __JobHandle.IsDone__, calling __Complete__ is required to get back ownership of the NativeContainers to the main thread. Calling Complete also cleans up state in the jobs debugger. Not doing so introduces a memory leak, this also applies if you schedule new jobs every frame with a dependency on the previous frame's job.
