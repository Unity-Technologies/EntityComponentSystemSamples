# C# Job System

> **Note**: Most of the C# Job System content has moved to the official [Unity Manual](https://docs.unity3d.com/Manual/JobSystem.html). What remains below relates to ECS specific information about the C# Job System that is not in the manual.

## Concurrent NativeContainer types

Some `NativeContainer` types also have special rules for allowing safe and deterministic write access from ParallelFor jobs. As an example, the method `NativeHashMap.Concurrent` lets you add items in parallel from [IJobParallelFor](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Jobs.IJobParallelFor.html).
