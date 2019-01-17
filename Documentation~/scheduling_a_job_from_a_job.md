# Scheduling a job from a job - why not?

We have a couple of important principles that drive our design.

* Determinism by default: Determinism enables networked games, replay and debugging tools.
* Safe: Race conditions are immediately reported, this makes writing jobified code significantly more approachable and simple.

These two principles applied result in some choices and restrictions that we enforce.

## Jobs can only be completed on the main thread - but why?

If you were to call __JobHandle.Complete__ that leads to impossible to solve job scheduler deadlocks.
(We have tried this over the last couple years with the Unity C++ code base, and every single case has resulted in tears and us reverting such patterns in our code.) The deadlocks are rare but provably impossible to solve in all cases, they are heavily dependent on the timing of jobs.

## Jobs can only be scheduled on the main thread - but why?

If you were to simply schedule a job from another job, but not call JobHandle.Complete from the job, then there is no way to guarantee determinism. The main thread has to call JobHandle.Complete(), but who passes that JobHandle to the main thread? How do you know the job that schedules the other job has already executed?

In summary, first instinct is to simply schedule jobs from other jobs, and then wait for jobs within a job.
Yet experience tells us that this is always a bad idea. So the C# job system does not support it.

## OK, but how do I process workloads where I don't know the exact size upfront?

It's totally fine to schedule jobs conservatively and then simply exit early and do nothing if it turns out the number of actual elements to process, when the job executes, is much less than the conservative number of elements that was determined at schedule time. 

In fact this way of doing it leads to deterministic execution, and if the early exit can skip a whole batch of operations it's not really a performance issue.
Also, there is no possibility of causing internal job scheduler deadlocks.

For this purpose using __IJobParallelForBatch__ as opposed to __IJobParallelFor__ can be very useful since you can exit early on a whole batch.
```
    public interface IJobParallelForBatch
    {
        void Execute(int startIndex, int count);
    }
```
TODO: CODE EXAMPLE for sorting?
