# Chunk Iteration

## What is an ArchetypeChunk?

1. An [EntityManager](entity_manager.md) stores component data in fixed sized blocks of 16KB.
2. Each of those 16KB blocks is referred to as a chunk.
3. A `Chunk` (internal) struct contains information about what is contained in that block.
4. An [EntityArchetype](entity_archetype.md) represents a unique collection of `Chunk`
5. A chunk can only exist in one archetype.
6. An `ArchetypeChunk` (public) struct is a pointer to a specific Chunk.

## What is contained in a Chunk?

1. A pointer to the specific `Archetype` this chunk matches.
2. A count and capacity for the component data parallel arrays stored in the chunk. Capacity is calculated by taking the size of the chunk (16KB) and dividing it by the sum of the sizes of each component in the Archetype.
3. An array of indices to specific values of [SharedComponentData](shared_component_data.md). A chunk cannot hold more than one specific value for a given type of `SharedComponentData`. (An Archetype is defined by the combination of `ComponentData` *types* and `SharedComponentData` *values*.)
4. A `ChangeVersion` for each component type in the Archetype representing the last time *any* value of a particular component type was potentially changed in this specific chunk.

## Reading and writing component data in chunks

The general process for reading or writing component data (which is always stored in chunks.)

1. Find the specific archetypes which contain the components.
2. Declare the read/write status of each of the requested components.
3. For each archetype, iterate over each of its chunks.
4. For each chunk, iterate the chunk's parallel component data arrays.
5. Update the specific element(s) in the chunk's parallel component data arrays.

There are two recommended methods for accomplishing this:

## IJobProcessComponentData

`IJobProcessComponentData` is a utility which simplifies the process of reading and writing component data for many common uses.

e.g.
```C#
public class RotationSpeedSystem : JobComponentSystem
{
    [BurstCompile]
    struct RotationSpeedRotation : IJobProcessComponentData<Rotation, RotationSpeed>
    {
        public float dt;

        public void Execute(ref Rotation rotation, [ReadOnly]ref RotationSpeed speed)
        {
            rotation.value = math.mul(math.normalize(rotation.value), quaternion.axisAngle(math.up(), speed.speed * dt));
        }
    }

    // Any previously scheduled jobs reading/writing from Rotation or writing to RotationSpeed 
    // will automatically be included in the inputDeps dependency.
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new RotationSpeedRotation() { dt = Time.deltaTime };
        return job.Schedule(this, inputDeps);
    } 
}
```

Which corresponds to the general process above, as:

1. Find the specific archetypes which contain the components.
    ```
    [BurstCompile]
    struct RotationSpeedRotation : IJobProcessComponentData<Rotation, RotationSpeed>
    ```
    The generic arguments define the required components to find all matching Archetypes. In this case, it means find all Archetypes which have a `Rotation` and a `RotationSpeed` component.
    
    A subtractive attribute may also be used:
    ```
    [RequireSubtractiveComponent(typeof(Frozen))]
    [BurstCompile]
    struct RotationSpeedRotation : IJobProcessComponentData<Rotation, RotationSpeed>
    ```
    ...which means find all the Archetypes which have a `Rotation` and a `RotationSpeed` component, but do *not* have a `Frozen` component.
    
    An component which is required, but will not be read or written, can also be declared:
    ```
    [RequireComponentTag(typeof(Gravity))]
    [BurstCompile]
    struct RotationSpeedRotation : IJobProcessComponentData<Rotation, RotationSpeed>
    ```
    ...which means find all the Archetypes which have a `Rotation` and a `RotationSpeed` component and *also* must have a `Gravity` component (but will not be read or written to.)

2. Declare the read/write status of each of the requested components.
    A requested component type can be declared `ReadOnly` through the use of an attribute:
    ```
    public void Execute(ref Rotation rotation, [ReadOnly]ref RotationSpeed speed)
    ```
    ...which means that `RotationSpeed` will not be written to, so the corresponding `ChangeVersion` for `RotationSpeed` in any chunk it is found in will not be updated.
    Conversely, any writable component will have it's `ChangeVersion` bumped in every matching chunk. (Regardless of whether data is actually written.)

3. For each archetype, iterate over each of its chunks.
    Internally, `IJobProcessComponentData` will iterate over each archetype, and over each chunk within each archetype.
    
    Unchanged chunks can be skipped by adding attributes to Execute() parameters. e.g.
    ```
    public void Execute([ChangedFilter] ref Rotation rotation, [ReadOnly]ref RotationSpeed speed)
    ```
    ...which means only iterate over chunks which have `Rotation` version numbers that have changed since the last system update.
    ...which means chunks whose component values of that specific type were referred to as writable at any point between the last system update and the current system update. (The values are not checked to see if they were actually changed.)

4. For each chunk, iterate the chunk's parallel component data arrays.
    Internally, `IJobProcessComponentData` iterates each parallel component data array, and passes the requested component type values to the Execute method.
   
5. Update the specific element(s) in the chunk's parallel component data arrays.
    For each set of elements within the parallel component data arrays, within every matching chunk, in every matching archetype, the Execute method is called.
    ```
    public void Execute(ref Rotation rotation, [ReadOnly]ref RotationSpeed speed)
    {
        rotation.value = math.mul(math.normalize(rotation.value), quaternion.axisAngle(math.up(), speed.speed * dt));
    }
    ```
   
    The outer loop of Execute method is split reasonably across available threads. (It is not one job per Execute call.)

## ArchetypeChunk access

The other recommended method for reading and writing component data is by iterating ArchetypeChunk directly. This method is recommended for all cases which do not fit the simplified `IJobProcessComponentData` model.

This method is more explicit and represents the most direct access to the data, as it is actually stored.

The equivalent process to the `IJobProcessComponentData` version above is:
```C#
public class RotationSpeedSystem : JobComponentSystem
{
    [BurstCompile]
    struct RotationSpeedRotation : IJobChunk
    {
        public ArchetypeChunkComponentType<Rotation> RotationType;
        public ArchetypeChunkComponentType<RotationSpeed> RotationSpeedType;
        public float dt;

        public void Execute(ArchetypeChunk chunk, int chunkIndex)
        {
            var chunkRotation = chunk.GetNativeArray(RotationType);
            var chunkSpeed = chunk.GetNativeArray(RotationSpeedType);
            var instanceCount = chunk.Count;

            for (int i = 0; i < instanceCount; i++)
            {
                var rotation = chunkRotation[i];
                var speed = chunkSpeed[i];

                rotation.Value = math.mul(math.normalize(rotation.Value), quaternion.AxisAngle(math.up(), speed.Value * dt));

                chunkRotation[i] = rotation;
            }
        }
    }

    ComponentGroup m_RotationSpeedRotationGroup;

    protected override void OnCreateManager()
    {
        var query = new EntityArchetypeQuery
        {
            All = new ComponentType[]{ typeof(Rotation), typeof(RotationSpeed) }
        };
        m_RotationSpeedRotationGroup = GetComponentGroup(query);
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var rotationType = GetArchetypeChunkComponentType<Rotation>();
        var rotationSpeedType = GetArchetypeChunkComponentType<RotationSpeed>(true);

        var chunks = m_RotationSpeedRotationGroup.CreateArchetypeChunkArray(Allocator.TempJob);
        
        var rotationsSpeedRotationJob = new RotationSpeedRotation
        {
            RotationType = rotationType,
            RotationSpeedType = rotationSpeedType,
            dt = Time.deltaTime
        };
        var rotationSpeedRotationJobHandle = rotationsSpeedRotationJob.Schedule(m_RotationSpeedRotationGroup,inputDeps);
        return rotationSpeedRotationJobHandle;
    } 
}
```

In terms of the general process above:

1. Find the specific archetypes which contain the components.

    ```
    ComponentGroup m_RotationSpeedRotationGroup;
    
    protected override void OnCreateManager()
    {
        var query = new EntityArchetypeQuery
        {
            All = new ComponentType[]{ typeof(Rotation), typeof(RotationSpeed) }
        };
        m_RotationSpeedRotationGroup = GetComponentGroup(query);
    }
    ```

    - Store a ComponentGroup on the ComponentSystem or JobComponentSystem
    - Initialize it with GetComponentGroup()
    - Pass in an EntityArchetypeQuery which defines which Archetypes should match.
        - `All` = All components in the array must exist in the Archetype
        - `Any` = Any of the components in the array must exist in the Archetype (at least one)
        - `None` = None of the components in the array can exist in the Archetype

        **Note:** Do not include completely optional components in the EntityArchetypeQuery. To handle optional components, use the `chunk.Has<T>()` method inside `IJobChunk.Execute()` to determine whether the current ArchetypeChunk has the optional Component or not. Since all Entities within the same chunk have the same Components, you only need to check whether an optional Component exists once per chunk -- not once per Entity.
       

    The following code example defines an EntityArchetypeQuery for a System in its `OnCreateManager()` function:
    ```
    ComponentGroup m_RotationSpeedRotationGroup;
    
    protected override void OnCreateManager()
    {
        var query = new EntityArchetypeQuery
        {
            None = new ComponentType[]{ typeof(Frozen) },
            All = new ComponentType[]{ typeof(Rotation), typeof(RotationSpeed) }
        };
        m_RotationSpeedRotationGroup = GetComponentGroup(query);
    }
    ```
    ...this query means, find all archetypes that have `Rotation` and `RotationSpeed` but *not* a `Frozen` component.

    In addition, you can logically OR multiple queries together by providing multiple `EntityArchetypeQuery` in an array. e.g.
    ```
    var query0 = new EntityArchetypeQuery
    {
        All = new ComponentType[]{ typeof(Rotation) }
    };
    var query1 = new EntityArchetypeQuery
    {
        All = new ComponentType[]{ typeof(RotationSpeed) }
    };
    
    m_RotationSpeedRotationGroup = GetComponentGroup(new EntityArchetypeQuery[]{ query0, query1 });
    ```
    ...means match any Archetype that has a `Rotation` component *or* a `RotationSpeed` compon

    The resulting `ComponentGroup` represents a collection of matching archetypes.

2. Declare the read/write status of each of the requested components.

    Each type must be stored in ArchetypeChunkComponentType<> container so that it can be accessed from the job (and the Burst compiler)
    Request the type container:
    ```
    var rotationType = GetArchetypeChunkComponentType<Rotation>();
    var rotationSpeedType = GetArchetypeChunkComponentType<RotationSpeed>(true);
    ```
    The container accepts one parameter, where true = ReadOnly.
    
3. For each archetype, iterate over each of its chunks.

    Using `IJobChunk` specifies you will be iterating over all the chunks in all the archetypes of a `ComponentGroup`
    ```
    struct RotationSpeedRotation : IJobChunk
    ```
    
    Pass in the `ComponentGroup` when the job is scheduled:
    ```
    var rotationSpeedRotationJobHandle = rotationsSpeedRotationJob.Schedule(m_RotationSpeedRotationGroup,inputDeps);
    ```
    
    And the Execute method will passed the appropriate Chunk:
    ```
    public void Execute(ArchetypeChunk chunk, int chunkIndex)
    ```
    
    You can also request all the chunks explicitly in a NativeArray and process them as an `IJobParallelFor`. This method is recommended if you need to manage chunks in some way that is not appropriate for the simplified model of simply iterating over all the Chunks in a ComponentGroup. As in:
    ```C#
    public class RotationSpeedSystem : JobComponentSystem
    {
        struct RotationSpeed : IComponentData
        {
            public float Value;
        }
        
        [BurstCompile]
        struct RotationSpeedRotation : IJobParallelFor
        {
            [DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk> Chunks;
            public ArchetypeChunkComponentType<Rotation> RotationType;
            public ArchetypeChunkComponentType<RotationSpeed> RotationSpeedType;
            public float dt;
    
            public void Execute(int chunkIndex)
            {
                var chunk = Chunks[chunkIndex];
                var chunkRotation = chunk.GetNativeArray(RotationType);
                var chunkSpeed = chunk.GetNativeArray(RotationSpeedType);
                var instanceCount = chunk.Count;
    
                for (int i = 0; i < instanceCount; i++)
                {
                    var rotation = chunkRotation[i];
                    var speed = chunkSpeed[i];
    
                    rotation.Value = math.mul(math.normalize(rotation.Value), quaternion.AxisAngle(math.up(), speed.Value * dt));
    
                    chunkRotation[i] = rotation;
                }
            }
        }
    
        ComponentGroup m_RotationSpeedRotationGroup;
    
        protected override void OnCreateManager()
        {
            var query = new EntityArchetypeQuery
            {
                All = new ComponentType[]{ typeof(Rotation), typeof(RotationSpeed) }
            };
            m_RotationSpeedRotationGroup = GetComponentGroup(query);
        }
    
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var rotationType = GetArchetypeChunkComponentType<Rotation>();
            var rotationSpeedType = GetArchetypeChunkComponentType<RotationSpeed>(true);
    
            var chunks = m_RotationSpeedRotationGroup.CreateArchetypeChunkArray(Allocator.TempJob);
            
            var rotationsSpeedRotationJob = new RotationSpeedRotation
            {
                Chunks = chunks,
                RotationType = rotationType,
                RotationSpeedType = rotationSpeedType,
                dt = Time.deltaTime
            };
            var rotationSpeedRotationJobHandle = rotationsSpeedRotationJob.Schedule(chunks.Length,32,inputDeps);
            return rotationSpeedRotationJobHandle;
        } 
    }
    ```

4. For each chunk, iterate the chunk's parallel component data arrays.

    Within a chunk, request the parallel arrays of component data directly by passing the type container:
    ```
    var chunkRotation = chunk.GetNativeArray(RotationType);
    var chunkSpeed = chunk.GetNativeArray(RotationSpeedType);
    ```
    
    For optional components, check for existence of those components using the chunk Has method:
    ```
    var hasChunkRotation = chunk.Has(RotationType);
    ```
    Note: This is only relevant in the case of using the `Any` filter or the component type not being specified in the filter (zero or more)

5. Update the specific element(s) in the chunk's parallel component data arrays.

   Retrieve the length of the parallel arrays via `Chunk.Count`, and replace writable values within the array.
   Note: NativeArray does not support ref returns. Assign a complete instance of the type to the index of the NativeArray.

   ```
    var instanceCount = chunk.Count;

    for (int i = 0; i < instanceCount; i++)
    {
        var rotation = chunkRotation[i];
        var speed = chunkSpeed[i];

        rotation.Value = math.mul(math.normalize(rotation.Value), quaternion.AxisAngle(math.up(), speed.Value * dt));

        chunkRotation[i] = rotation;
   }
   ```

Like `IJobProcessComponentData`, chunks can also be filtered (explicitly) by change versions.

1. Each ComponentSystem or JobComponentSystem stores a value `LastSystemVersion`. This is the value of `GlobalSystemVersion` was the previous time this system was updated.
2. If any Component type in a chunk has a version greater than `LastSystemVersion` it means it was (potentially) changed between the last update and the current update.

For instance,

```C#
    [BurstCompile]
    struct RotationSpeedRotation : IJobChunk
    {
        public uint LastSystemVersion
        public ArchetypeChunkComponentType<Rotation> RotationType;
        public ArchetypeChunkComponentType<RotationSpeed> RotationSpeedType;
        public float dt;

        public void Execute(ArchetypeChunk chunk, int chunkIndex)
        {
            var rotationSpeedChanged = chunk.DidChange(RotationSpeedType,LastSystemVersion);
            var rotationChanged = chunk.DidChange(RotationType,LastSystemVersion);
            if (!(rotationSpeedChanged || rotationChanged))
                return;
                
            var chunkRotation = chunk.GetNativeArray(RotationType);
            var chunkSpeed = chunk.GetNativeArray(RotationSpeedType);
            var instanceCount = chunk.Count;

            for (int i = 0; i < instanceCount; i++)
            {
                var rotation = chunkRotation[i];
                var speed = chunkSpeed[i];

                rotation.Value = math.mul(math.normalize(rotation.Value), quaternion.AxisAngle(math.up(), speed.Value * dt));

                chunkRotation[i] = rotation;
            }
        }
    }
```

...will reject any chunk that has no potential changes to either `RotationType` or `RotationSpeedType` since the last update

[Back to Unity Data-Oriented reference](reference.md)
