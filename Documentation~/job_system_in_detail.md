# C# Job System features in detail

> **Note**: The main content of this page has migrated to the [Unity Data-Oriented reference](reference.md). C# Job System related features are listed below in alphabetical order, with a short description and links to further information about it. This page is not an exhaustive list and can be added to over time as the C# Job System, and its related documentation expands. If you spot something that is out-of-date or broken links, then make sure to let us know in the [forums](http://unity3d.com/performance-by-default) or as an [issue](https://github.com/Unity-Technologies/EntityComponentSystemSamples/issues/new) in the repository.

## Allocator 

When creating a `NativeContainer`, you must specify the type of memory allocation you need. The allocation type depends on the length of time the job runs. This way you can tailor the allocation to get the best performance possible in each situation.

There are three [Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) types for `NativeContainer` memory allocation and release. You need to specify the appropriate one when instantiating your `NativeContainer`.

For more information, see the [NativeContainer](https://docs.unity3d.com/Manual/JobSystemNativeContainer.html) manual page - see "NativeContainer Allocator."

## AtomicSafetyHandle

TODO

##Batch

TODO

##DisposeSentinel

The `DisposeSentinel` detects memory leaks and gives you an error if you have not correctly freed your memory. 

## 

TODO

## NativeArray

TODO

## NativeContainer

TODO

## IJob



##IJobParallelFor

TODO

## IJobParallelforTransform job

TODO

## Safety system

TODO

## Further information

For more information on C# Job System features, see the [C# Job System](https://docs.unity3d.com/Manual/JobSystem.html) Manual.