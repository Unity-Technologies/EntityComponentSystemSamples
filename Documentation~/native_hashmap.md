# NativeHashMap

WIP

[NativeContainer](https://docs.unity3d.com/Manual/JobSystemNativeContainer.html) created as part of Unity ECS.

It has special rules for allowing safe and deterministic write access from [ParallelFor jobs](https://docs.unity3d.com/Manual/JobSystemParallelForJobs.html). The `NativeHashMap.Concurrent` method lets you add items in parallel from [IJobParallelFor](https://docs.unity3d.com/ScriptReference/Unity.Jobs.IJobParallelFor.html).

See also: [Wikipedia - Hash table](https://en.wikipedia.org/wiki/Hash_table).

[Back to Unity Data-Oriented reference](reference.md)

