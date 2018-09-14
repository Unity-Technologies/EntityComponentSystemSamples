# C# Job System

> **Note**: Most of the C# Job System content has moved to the official [Unity Manual](https://docs.unity3d.com/Manual/JobSystem.html). What remains below relates to ECS specific information about the C# Job System that is not in the manual.

## Concurrent NativeContainer types

Some `NativeContainer` types also have special rules for allowing safe and deterministic write access from ParallelFor jobs. As an example, the method `NativeHashMap.Concurrent` lets you add items in parallel from [IJobParallelFor](https://docs.unity3d.com/2018.1/Documentation/ScriptReference/Unity.Jobs.IJobParallelFor.html).

## Concurrent EntityCommandBuffers

When using an `EntityCommandBuffer` to issue `EntityManager` commands from ParallelFor jobs, the `EntityCommandBuffer.Concurrent` interface must be used in order to guarantee thread safety and deterministic playback. The public methods in this interface take an extra `jobIndex` parameter, which is used to play back the recorded commands in a deterministic order. `jobIndex` must be a unique ID for each job. For performance reasons, `jobIndex` should be related to the (increasing) `index` values passed to `IJobParallelFor.Execute()`. Unless you *really* know what you're doing, using `index` as `jobIndex` is the safest choice. Using other `jobIndex` values will produce correct output, but can have severe performance implications in some cases.
