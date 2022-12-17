<!---
This file has been generated from jobs.src.md
Do not modify manually! Use the following tool:
https://github.com/Unity-Technologies/dots-tutorial-processor
-->
# **IJob**

```c#
// An example job which increments all the numbers of an array.
public struct IncrementJob : IJob
{
    // The data which a job needs to use should all
    // be included as fields of the struct.
    public NativeArray<float> Nums;
    public float Increment;

    // Execute() is called when the job runs.
    public void Execute()
    {
        for (int i = 0; i < Nums.Length; i++)
        {
            Nums[i] += Increment;
        }
    }
}

// A system that schedules the IJob.
[BurstCompile]
public partial struct MySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state) { }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new IncrementJob {
            Nums = new NativeArray<float>(1000, state.WorldUpdateAllocator),
            Increment = 5f
        };

        JobHandle handle = job.Schedule();
        handle.Complete();
    }
}
```

<br>

# **IJobParallelFor**

```c#
// An example job which increments all the numbers of an array in parallel.
public struct IncrementParallelJob : IJobParallelFor
{
    // The data which a job needs to use must all
    // be included as fields of the struct.
    public NativeArray<float> Nums;
    public float Increment;

    // Execute(int) is called when the job runs.
    public void Execute(int index)
    {
        Nums[index] += Increment;
    }
}

// A system that schedules the IJobParallelFor.
[BurstCompile]
public partial struct MySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state) { }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new IncrementParallelJob {
            Nums = new NativeArray<float>(1000, state.WorldUpdateAllocator),
            Increment = 5f
        };

        JobHandle handle = job.Schedule(
            job.Nums.Length,          // number of times to call Execute
            64);     // split the calls into batches of 64
        handle.Complete();
    }
}
```

<br>

# **IJobChunk**

```c#
// An example IJobChunk.
[BurstCompile]
public partial struct MyIJobChunk : IJobChunk
{
    // The job needs type handles for each component type
    // it will access from the chunks.
    public ComponentTypeHandle<Foo> FooHandle;

    // Handles for components that will only be read should be
    // marked with [ReadOnly].
    [ReadOnly] public ComponentTypeHandle<Bar> BarHandle;

    // The entity type handle is needed if we
    // want to read the entity ID's.
    public EntityTypeHandle EntityHandle;

    // Jobs should not use an EntityManager to create and modify
    // entities directly. Instead, a job can record commands into
    // an EntityCommandBuffer to be played back later on the
    // main thread at some point after the job has been completed.
    // If the job will be scheduled with ScheduleParallel(),
    // we must use an EntityCommandBuffer.ParallelWriter.
    public EntityCommandBuffer.ParallelWriter Ecb;

    // When this job runs, Execute() will be called once for each
    // chunk matching the query that was passed to Schedule().

    // The useEnableMask param is true if any of the
    // entities in the chunk have disabled components
    // of the query. In other words, this param is true
    // if any entities in the chunk should be skipped over.

    // The chunkEnabledMask identifies which entities
    // have all components of the query enabled, i.e. which entities
    // should be processed:
    //   - A set bit indicates the entity should be processed.
    //   - A cleared bit indicates the entity has one or more
    //     disabled components and so should be skipped.
    [BurstCompile]
    public void Execute(in ArchetypeChunk chunk,
            int unfilteredChunkIndex,
            bool useEnableMask,
            in v128 chunkEnabledMask)
    {
        // Get the entity ID and component arrays from the chunk.
        NativeArray<Entity> entities =
                chunk.GetNativeArray(ref EntityHandle);
        NativeArray<Foo> foos =
                chunk.GetNativeArray(ref FooHandle);
        NativeArray<Bar> bars =
                chunk.GetNativeArray(ref BarHandle);

        // The ChunkEntityEnumerator helps us loop over
        // the entities of the chunk, but only those that
        // match the query (accounting for disabled components).
        var enumerator = new ChunkEntityEnumerator(
                useEnableMask,
                chunkEnabledMask,
                chunk.ChunkEntityCount);

        // Loop over all entities in the chunk that match the query.
        while (enumerator.NextEntityIndex(out var i))
        {
            // Read the entity ID and component values.
            var entity = entities[i];
            var foo = foos[i];
            var bar = bars[i];

            // If the Bar value meets a criteria, we
            // record a command in the ECB to remove it.
            if (bar.Value < 0)
            {
                Ecb.RemoveComponent<Bar>(unfilteredChunkIndex, entity);
            }

            // Set the Foo value.
            foos[i] = new Foo { };
        }
    }
}
```

The `unfilteredChunkIndex` is the index of the chunk in the sequence of all chunks matching the query: the first chunk matching the query is index 0, the second is index 1, and so forth. This value is mainly useful as a *sort key* passed to the methods of `EntityCommandBuffer.ParallelWriter`. Each recorded command includes a sortKey, and in playback, the commands are sorted by these keys before the commands are executed. This sorting effectively guarantees the commands will execute in a deterministic order even though the original recorded order of the commands was non-deterministic.

```c#
// A system that schedules and completes the above IJobChunk.
[BurstCompile]
public partial struct MySystem : ISystem
{
    private EntityQuery myQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<Foo, Bar, Apple>();
        builder.WithNone<Banana>();
        myQuery = state.GetEntityQuery(builder);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Get an EntityCommandBuffer from
        // the BeginSimulationEntityCommandBufferSystem.
        var ecbSingleton =
            SystemAPI.GetSingleton<
                BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // Create the job.
        var job = new MyIJobChunk
        {
            FooHandle = state.GetComponentTypeHandle<Foo>(false),
            BarHandle = state.GetComponentTypeHandle<Bar>(true),
            Ecb = ecb.AsParallelWriter()
        };

        // Schedule the job.
        // By calling ScheduleParallel() instead of Schedule(),
        // the chunks matching the job's query will be split up
        // into batches, and these batches may be processed
        // in parallel by the worker threads.
        // We pass state.Dependency to ensure that this job depends upon
        // any overlapping jobs scheduled in prior system updates.
        // We assign the returned handle to state.Dependency to ensure
        // that this job is passed as a dependency to other systems.
        state.Dependency = job.ScheduleParallel(myQuery, state.Dependency);
    }
}
```

Before each update, a system completes its `state.Dependency` property, so jobs scheduled in a system update will be completed, at the latest, right before the system's next update. Immediately after `state.Dependency` is completed, it's assigned a new value: a job handle combining the `state.Dependency` properties of all other systems that access any of the same component types as this system.

Although we could complete the new job immediately after scheduling, we should generally rely upon `state.Dependency` to defer completion of the job to the time when it actually needs to be finished.

<br>

# **IJobEntity**

An `IJobEntity` is more concise than its `IJobChunk` equivalent because its source generation takes care of some boilerplate:

```c#
// An example IJobEntity that is functionally equivalent to the IJobChunk above.

// Only entities having the Apple component type will match the implicit query
// even though we do not access the Apple component values.
[WithAll(typeof(Apple))]
// Only entities NOT having the Banana component type will match the implicit query.
[WithNone(typeof(Banana))]
[BurstCompile]
public partial struct MyIJobEntity : IJobEntity
{
    // Thanks to source generation, an IJobEntity gets the type handles
    // it needs automatically, so we do not include them manually.

    // EntityCommandBuffers and other fields still must
    // be included manually.
    public EntityCommandBuffer.ParallelWriter Ecb;

    // Source generation will create an EntityQuery based on the
    // parameters of Execute(). In this case, the generated query will
    // match all entities having a Foo and Bar component.
    //   - When this job runs, Execute() will be called once
    //     for each entity matching the query.
    //   - Any entity with a disabled Foo or Bar will be skipped.
    //   - 'ref' param components are read-write
    //   - 'in' param components are read-only
    //   - We need to pass the chunk index as a sortKey to methods of
    //     the EntityCommandBuffer.ParallelWriter, so we include an
    //     int parameter with the [ChunkIndexInQuery] attribute.
    [BurstCompile]
    public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref Foo foo, in Bar bar)
    {
        // If the Bar value meets this criteria, we
        // record a command in the ECB to remove it.
        if (bar.Value < 0)
        {
            Ecb.RemoveComponent<Bar>(chunkIndex, entity);
        }

        // Set the Foo value.
        foo = new Foo { };
    }
}
```

```c#
// A system that schedules and completes the above IJobEntity.
[BurstCompile]
public partial struct MySystem : ISystem
{
    // We don't need to create the query manually because source generation
    // creates one inferred from the IJobEntity's attributes and Execute params.

    [BurstCompile]
    public void OnCreate(ref SystemState state) { }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Get an EntityCommandBuffer from the BeginSimulationEntityCommandBufferSystem.
        var ecbSingleton = SystemAPI.GetSingleton<
                BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // Create the job.
        var job = new MyIJobEntity
        {
            Ecb = ecb.AsParallelWriter()
        };

        // Schedule the job. Source generation creates and passes the query implicitly.
        state.Dependency = job.Schedule(state.Dependency);
    }
}
```

<br/>
