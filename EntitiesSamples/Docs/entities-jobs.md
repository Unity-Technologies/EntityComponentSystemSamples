
# Accessing entities in jobs

You can offload the processing of entity data to worker threads with the [C# Job System](https://docs.unity3d.com/Manual/JobSystem.html). The Entities package has two interfaces for defining jobs that access entities:

- [`IJobChunk`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.IJobChunk.html), whose `Execute()` method is called once for each individual chunk matching the query.
- [`IJobEntity`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.IJobEntity.html), whose `Execute()` method is called once for each entity entity matching the query. 

&#x1F579; *[See examples of IJobChunk](./examples/jobs.md#ijobchunk) [and IJobEntity](./examples/jobs.md#ijobentity).*

Although `IJobEntity` is generally more convenient to write and use, `IJobChunk` provides more precise control. In most cases, their performance is identical for equivalent work.

| &#x1F4DD; NOTE |
| :- |
| `IJobEntity` is not actually a 'real' job type: source generation extends an `IJobEntity` struct with an implementation of `IJobChunk`. So in fact, a `IJobEntity` is ultimately scheduled as an `IJobChunk`. |

It is not safe to make [structural changes](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/concepts-structural-changes.html) in a job, so normally you should only make structural changes on the main thread. To work around this restriction, a job can record structural change commands in an [`EntityCommandBuffer`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityCommandBuffer.html), and then these commands can be played back later on the main thread.

To split the work of an `IJobChunk` or `IJobEntity` across multiple threads, schedule the job by calling `ScheduleParallel()` instead of `Schedule()`. When you use `ScheduleParallel()`, the chunks matching the query will be split into separate batches, and these batches will be farmed out to the worker threads.

<br>

## Sync points

A **'synchronization point'** operation is an operation that cannot safely be performed concurrently with the scheduled jobs which may access entities and components, and so these operations must first complete the jobs. For example, calling `EntityManager.CreateEntity()` will first complete all currently scheduled jobs which access any entities and components. Likewise, the `EntityQuery` methods `ToComponentDataArray<T>()`, `ToEntityArray()`, and `ToArchetypeChunkArray()` must first complete any currently scheduled jobs which access any of the same components as the query.

In many cases, these synchronization points will also **'invalidate'** existing instances of a few types, namely [`DynamicBuffer`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.DynamicBuffer-1.html) and [`ComponentLookup<T>`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.ComponentLookup-1.html). When an instance is invalidated, calling its methods will throw safety check exceptions. If an instance you need to still use gets invalidated, you must retrieve a new instance to replace it.

<br>

## Component safety handles

Just like the native collections, each component type has an associated job safety handle for each world. The implication is that, for any two jobs which access the same component type in a world, the safety checks won't let the jobs be scheduled concurrently. For example, when we try scheduling a job that accesses component type *Foo*, the safety checks will throw an exception if an already scheduled job also accesses component type *Foo*. To avoid this exception, the already scheduled job must be completed before scheduling the new job, or the new job must depend upon the already scheduled job. 

| &#x1F4DD; NOTE |
| :- |
| It's safe for two jobs to be scheduled concurrently if they both have *read-only* access of the same component type. For any component type in your job that is not ever written, be sure to inform the safety checks by marking the component type handle with the [`ReadOnly`](https://docs.unity3d.com/ScriptReference/Unity.Collections.ReadOnlyAttribute.html) attribute. |

<br>

## SystemState.Dependency

When we schedule a job in a system, we want it to depend upon any currently scheduled jobs that might conflict with the new job, even if those jobs were scheduled in other systems. To arrange this, we use the job handle property `Dependency` of [`SystemState`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.SystemState.html).

Immediately before a system updates:

1. ...the system's `Dependency` property is completed
2. ...and then assigned a combination of the `Dependency` handles of all other systems which access any of the same component types as this system. For example, for a system which accesses the *Foo* and *Bar* component type, the `Dependency` of all other systems in the world which also access either *Foo* or *Bar* will be included in the combination job handle.

You're then expected to do two things in every system:

1. All jobs scheduled in a system update should (directly or indirectly) depend upon the job handle that was assigned to `Dependency` right before the update.
1. Before a system update returns, the `Dependency` property should be assigned a handle that includes all the jobs scheduled in that update.

As long as you follow these two rules, every job scheduled in a system update will depend upon all jobs scheduled in other systems which might access any of the same component types.

| &#x26A0; IMPORTANT |
| :- |
| Systems do not track which [native collections]() they use, so the `Dependency` property only accounts for component types, not native collections. Consequently, if two systems both schedule jobs which use the same native collection, their `Dependency` properties will not necessarily be combined into the job handle assigned to the `Dependency` property of the other, and so the jobs of the different systems will not depend upon each other as they should. In these scenarios, you *could* manually share job handles between the systems, but the better solution is to store the native collection in a component: if both systems access the collection through the same component type, the jobs scheduled in both systems should then depend upon each other (as long as you follow the `Dependency` rules described above). |

<br>

## ComponentLookup\<T\>

We can randomly access the components of individual entities through an `EntityManager`, but we generally shouldn't use an `EntityManager` in jobs. Instead, we should use a type called [`ComponentLookup<T>`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.ComponentLookup-1.html), which can get and set component values by entity ID. We can also get dynamic buffers by entity ID using [`BufferLookup<T>`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.BufferLookup-1.html).

| &#x26A0; IMPORTANT |
| :- |
| Keep in mind that looking up an entity by ID tends to incur the performance cost of cache misses, so it's generally a good idea to avoid lookups when you can. There are though, of course, many problems which require random lookups to solve, so by no means can random lookups be avoided entirely. Just avoid using them carelessly! |


The `ComponentLookup<T>` and `BufferLookup<T>` method [`HasComponent()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.ComponentLookup-1.HasComponent.html) returns true if the specified entity has the component type T. The [`TryGetComponent<T>()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.ComponentLookup-1.TryGetComponent.html) and [`TryGetBuffer<T>()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.BufferLookup-1.TryGetBuffer.html) methods do the same but also outputs the component value or buffer if it exists.

To test whether an entity simply exists, we can call [`Exists()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityStorageInfoLookup.Exists.html) of an [`EntityStorageInfoLookup`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityStorageInfoLookup.html). Indexing an `EntityStorageInfoLookup` returns an [`EntityStorageInfo`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityStorageInfo.html) struct, which includes a reference to the entity's chunk and its index within the chunk.


If a job needs to only *read* the components accessed through a `ComponentLookup<T>`, the `ComponentLookup<T>` field should be marked with the `ReadOnly` attribute to inform the job safety checks. The same is true for a `BufferLookup<T>`.

In a parallel-scheduled job, getting component values from a `ComponentLookup<T>` requires the field to be marked with the `ReadOnly` attribute. The safety checks do not allow setting component values through a `ComponentLookup<T>` in a parallel-scheduled job because safety cannot be guaranteed. However, you can fully *disable* the safety checks on the `ComponentLookup<T>` by marking it with the [`NativeDisableParallelForRestriction`](https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeDisableParallelForRestrictionAttribute.html) attribute. The same is true for a `BufferLookup<T>`. Just make sure that your code sets component values in a thread-safe manner!










