# EntityCommandBuffer

The `EntityCommandBuffer` class solves two important problems:

1. When you're in a job, you can't access the `EntityManager`.
2. When you access the `EntityManager` (to say, create an `Entity`) you invalidate all injected arrays and `ComponentGroup` objects.

The `EntityCommandBuffer` abstraction allows you to queue up changes (from either a job or from the main thread) so that they can take effect later on the main thread. There are two ways to use a `EntityCommandBuffer`:

1. `ComponentSystem` subclasses which update on the main thread have one available automatically called `PostUpdateCommands`. To use it, simply reference the attribute and queue up your changes. They will be automatically applied to the world immediately after you return from your system's `Update` function.

Here's an example from the two stick shooter sample:

```cs
PostUpdateCommands.CreateEntity(TwoStickBootstrap.BasicEnemyArchetype);
PostUpdateCommands.SetComponent(new Position2D { Value = spawnPosition });
PostUpdateCommands.SetComponent(new Heading2D { Value = new float2(0.0f, -1.0f) });
PostUpdateCommands.SetComponent(default(Enemy));
PostUpdateCommands.SetComponent(new Health { Value = TwoStickBootstrap.Settings.enemyInitialHealth });
PostUpdateCommands.SetComponent(new EnemyShootState { Cooldown = 0.5f });
PostUpdateCommands.SetComponent(new MoveSpeed { speed = TwoStickBootstrap.Settings.enemySpeed });
PostUpdateCommands.AddSharedComponent(TwoStickBootstrap.EnemyLook);
```

As you can see, the API is very similar to the `EntityManager` API. In this mode, it is helpful to think of the automatic `EntityCommandBuffer` as a convenience that allows you to prevent array invalidation inside your system while still making changes to the world.

1. For jobs, you must request `EntityCommandBuffer` from a barrier on the main thread, and pass them to jobs. When the `BarrierSystem` updates, the command buffers will play back on the main thread in the order they were created. This extra step is required so that memory management can be centralized and determinism of the generated entities and components can be guaranteed.

Again let's look at the two stick shooter sample to see how this works in practice.

## Barrier

First, a `BarrierSystem` is declared:

```cs
public class ShotSpawnBarrier : BarrierSystem
{}
```

There's no code in a `BarrierSystem`, it just serves as a synchronization point.

Next, we inject this barrier into the system that will request command buffers from it:

```cs
[Inject] private ShotSpawnBarrier m_ShotSpawnBarrier;
```

Now we can access the barrier when we're scheduling jobs and ask for command
buffers from it via `CreateCommandBuffer()`:

```cs
return new SpawnEnemyShots
{
    // ...
    CommandBuffer = m_ShotSpawnBarrier.CreateCommandBuffer(),
    // ...
}.Schedule(inputDeps);
```

In the job, we can use the command buffer normally:

```cs
CommandBuffer.CreateEntity(ShotArchetype);
CommandBuffer.SetComponent(spawn);
```

When the `BarrierSystem` updates, it will automatically play back the command buffers. It's worth noting that the `BarrierSystem` will take a dependency on any jobs spawned by systems that access it (so that it can know that the command buffers have been filled in fully). If you see bubbles in the frame, it may make sense to try moving the barrier later in the frame, if your game logic allows for this.

## Using EntityCommandBuffers from ParallelFor jobs

When using an `EntityCommandBuffer` to issue `EntityManager` commands from [ParallelFor jobs](https://docs.unity3d.com/Manual/JobSystemParallelForJobs.html), the `EntityCommandBuffer.Concurrent` interface is used to guarantee thread safety and deterministic playback. The public methods in this interface take an extra `jobIndex` parameter, which is used to playback the recorded commands in a deterministic order. The `jobIndex` must be a unique ID for each job. For performance reasons, `jobIndex` should be the (increasing) `index` values passed to `IJobParallelFor.Execute()`. Unless you *really* know what you're doing, using the `index` as `jobIndex` is the safest choice. Using other `jobIndex` values will produce the correct output, but can have severe performance implications in some cases.

[Back to Unity Data-Oriented reference](reference.md)