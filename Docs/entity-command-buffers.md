# Entity command buffers

We can defer changes to entities by recording commands into an [`EntityCommandBuffer`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityCommandBuffer.html). The recorded commands are executed later when we call [`Playback()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityCommandBuffer.Playback.html) on the main thread.

Deferring changes with an `EntityCommandBuffer` is particularly useful in jobs because jobs generally shouldn't directly make [structural changes](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/concepts-structural-changes.html) (*i.e.* create entities, destroy entities, add components, or remove components). Instead, jobs should record commands to be played back on the main thread after the job has been completed. `EntityCommandBuffer`'s can also help us avoid unnecessary [sync points](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/concepts-structural-changes.html#sync-points) by deferring structural changes to a few consolidated points of the frame rather than scattered across the frame.

An `EntityCommandBuffer` has many (but not all) of the same methods as `EntityManager`. The methods include:

|**`EntityCommandBuffer` method**|**Description**|
|---|---|
| [`CreateEntity()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityCommandBuffer.CreateEntity.html) | Records a command to create a new entity. Returns a [temporary](#temporary-entities) entity ID. |
| [`DestroyEntity()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityCommandBuffer.DestroyEntity.html) | Records a command to destroy an entity. |
| [`AddComponent<T>()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityCommandBuffer.AddComponent.html) | Records a command to add a component of type T to an entity. |
| [`RemoveComponent<T>()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityCommandBuffer.RemoveComponent.html) | Records a command to temove a component of type T from an entity. |
| [`SetComponent<T>()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityCommandBuffer.SetComponent.html) | Records a command to set a component value of type T. |
| [`AppendToBuffer()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityCommandBuffer.AppendToBuffer.html) | Records a command that will append an individual value to the end of the entity's existing buffer. |
| [`AddBuffer()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityCommandBuffer.AddBuffer.html) | Returns a `DynamicBuffer` which is stored in the recorded command, and the contents of this buffer will be copied to the entity's actual buffer when it is created in playback. Effectively, writing to the returned buffer allows you to set the initial contents of the component. |
| [`SetBuffer()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityCommandBuffer.SetBuffer.html)  | Like `AddBuffer()`, but it assumes the entity already has a buffer of the component type. In playback, the entity's already existing buffer content is overwritten by the contents of the returned buffer. |

&#x1F579; *[See examples of creating and using an `EntityCommandBuffer`](./examples/jobs.md#ijobchunk).*

| &#x1F4DD; NOTE |
| :- |
| Some `EntityManager` methods have no `EntityCommandBuffer` equivalent because an equivalent wouldn’t be feasible or make sense. For example, there are no `EntityCommandBuffer` methods for getting component values because *reading* data is not something that can be usefully deferred. |
| After it has been played back, an `EntityCommandBuffer` instance cannot be used for additional recording. If you need to record more commands, create a new, separate `EntityCommandBuffer` instance. |

<br>

## Job safety

Each `EntityCommandBuffer` has a job safety handle, so the safety checks will throw an exception if you:

- ...invoke the `EntityCommandBuffer`'s methods on the main thread while the `EntityCommandBuffer` is still in use by any currently scheduled jobs.
- ... or schedule a job that accesses an `EntityCommandBuffer` already in use by other currently scheduled jobs (*unless* the new job depends on those other jobs).

| &#x26A0; IMPORTANT |
| :- |
| You might be tempted to share a single `EntityCommandBuffer` instance across multiple jobs, but this is strongly discouraged. There are cases where it will work fine, but in many cases it will not. For example, using the same `EntityCommandBuffer.ParallelWriter` across multiple parallel jobs might lead to an unexpected playback order of the commands. Instead, **it’s virtually always best to create and use one `EntityCommandBuffer` per job**. Don't worry about a performance difference: recording and playing back a set of commands split across multiple `EntityCommandBuffer`'s is not really any more expensive than recording the same set of commands all into one `EntityCommandBuffer`. |

<br>

## Temporary entities

When you call the [`CreateEntity()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityCommandBuffer.CreateEntity.html) or [`Instantiate()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityCommandBuffer.Instantiate.html) methods of an `EntityCommandBuffer`, no new entity is created until the command is executed in playback, so the entity ID returned by these methods are *temporary* ID's, which have negative index numbers. Subsequent `AddComponent`, `SetComponent`, and `SetBuffer` commands of the same `EntityCommandBuffer` may use these temporary ID's. In playback, any temporary ID's in the recorded commands will be remapped to actual, existing entities.

| &#x26A0; IMPORTANT |
| :- |
| Because a temporary entity ID has no meaning outside of the `EntityCommandBuffer` instance from which it was created, it should *only* be used in subsequent method calls of the same `EntityCommandBuffer` instance. Do not, for example, use a temporary ID in recording a command of a different `EntityCommandBuffer` instance. |

<br>

## EntityCommandBuffer.ParallelWriter

To safely record commands from a parallel job, we need an [`EntityCommandBuffer.ParallelWriter`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityCommandBuffer.ParallelWriter.html), which is a wrapper around an underlying `EntityCommandBuffer`.

A `ParallelWriter` has most of the same methods as an `EntityCommandBuffer` itself, but the `ParallelWriter` methods all take an additional 'sort key' argument for the sake of determinism:

When an `EntityCommandBuffer.ParallelWriter` records commands in a parallel job, the order of commands recorded from different threads depends upon thread scheduling, making the order non-deterministic. This isn't ideal because:

- Deterministic code is generally easier to debug.
- Some netcode solutions depend upon determinism to produce consistent results across different machines. 

While the recording order of the commands cannot be deterministic, the *playback order* can be deterministic with a simple trick:

1. Each command records a 'sort key' integer passed as the first argument to each command method.
1. The `Playback()` method sorts the commands by their sort keys before executing the commands.

As long as the used sort keys map deterministically to each recorded command, the sort makes the playback order deterministic.

So in an `IJobEntity`, the sort key we generally want to use is the [`ChunkIndexInQuery`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.ChunkIndexInQuery.html), which is a unique value for every chunk. Because the sort is stable and because all entities of an individual chunk are processed together in a single thread, this index value is suitable as a sort key for the recorded commands. In an `IJobChunk`, we can use the equivalent `unfilteredChunkIndex` parameter of the `Execute` method.

<br>

## Multi-playback

If an `EntityCommandBuffer` is created with the `PlaybackPolicy.MultiPlayback` option, it's `Playback` method can be called more than once. Otherwise, calling `Playback` more than once will throw an exception. Multi-playback is mainly useful when you want to repeatedly spawn a set of entities.


<br>

## EntityCommandBufferSystem

An [`EntityCommandBufferSystem`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityCommandBufferSystem.html) is a system that provides a convenient way to defer `EntityCommandBuffer` playback. An `EntityCommandBuffer` instance created from an `EntityCommandBufferSystem` will be played back and disposed the next time the `EntityCommandBufferSystem` updates.

&#x1F579; *[See examples of creating and using an `EntityCommandBufferSystem`](./examples/components_systems.md#entitycommandbuffersystems).*

You rarely need to create any `EntityCommandBufferSystem`'s yourself because the automatic bootstrapping process puts these five into the default world:

- `BeginInitializationEntityCommandBufferSystem`
- `EndInitializationEntityCommandBufferSystem`
- `BeginSimulationEntityCommandBufferSystem`
- `EndSimulationEntityCommandBufferSystem`
- `BeginPresentationEntityCommandBufferSystem`

The `EndSimulationEntityCommandBufferSystem`, for example, is updated at the end of the `SimulationSystemGroup`. (Notice there's no *EndPresentationEntityCommandBufferSystem* at the end of the frame, but you can use `BeginInitializationEntityCommandBufferSystem` instead: the end of one frame and the beginning of the next are logically the same point in time).

| &#x26A0; IMPORTANT |
| :- |
| Do not manually play back and dispose an `EntityCommandBuffer` instance created by an `EntityCommandBufferSystem`: the `EntityCommandBufferSystem` will both play back and dispose the instance for you. |