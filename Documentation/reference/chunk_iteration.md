# Chunk Iteration

## Chunk implementation detail

The [ComponentData](component_data.md) for each [Entity](entity.md) is stored in what we internally refer to as a [chunk](https://en.wikipedia.org/wiki/Chunking_(computing)). `ComponentData` is laid out by stream. Meaning all components of type `A`, are tightly packed in an array. Followed by all components of type `B` etc.

A chunk is always linked to a specific [EntityArchetype](entity_archetype.md). Thus all Entities in one chunk follow the exact same memory layout. When iterating over components, memory access of components within a chunk is always completely linear, with no waste loaded into cache lines. This is a hard guarantee.

__ComponentDataArray__ is essentially a convenience index based iterator for a single component type;
First we iterate over all `EntityArchetype` structs compatible with the `ComponentGroup`; for each `EntityArchetype` iterating over all chunks compatible with it and for each chunk iterating over all entities in that chunk.

Once all entities of a chunk have been visited, we find the next matching chunk and iterate through those entities.

When entities are destroyed, we move up other entities into its place and then update the `Entity` table accordingly. This is required to make a hard guarantee on linear iteration of entities. The code moving the ComponentData into memory is highly optimized.

## Motivation

If, for example, there are three components, being `Position`, `Rotation`, and `Scale`, and the output of any combination of these three components should write to a `LocalToWorld` component. The approach using component group injection might look something like:

```
struct PositionToLocalToWorld
{
  ComponentDataArray<Position> Position;
  SubtractiveComponent<Rotation> Rotation;
  SubtractiveComponent<Scale> Scale;
  ComponentDataArray<LocalToWorld> LocalToWorld;
}
[Inject] PositionToLocalToWorld positionToLocalToWorld;

struct PositionRotationToLocalToWorld
{
  ComponentDataArray<Position> Position;
  ComponentDataArray<Rotation> Rotation;
  SubtractiveComponent<Scale> Scale;
  ComponentDataArray<LocalToWorld> LocalToWorld;
}
[Inject] PositionRotationToLocalToWorld positionRotationToLocalToWorld;

struct PositionRotationScaleToLocalToWorld
{
  ComponentDataArray<Position> Position;
  ComponentDataArray<Rotation> Rotation;
  ComponentDataArray<Scale> Scale;
  ComponentDataArray<LocalToWorld> LocalToWorld;
}
[Inject] PositionRotationScaleToLocalToWorld positionRotationScaleToLocalToWorld;

struct PositionScaleToLocalToWorld
{
  ComponentDataArray<Position> Position;
  SubtractiveComponent<Rotation> Rotation;
  ComponentDataArray<Scale> Scale;
  ComponentDataArray<LocalToWorld> LocalToWorld;
}
[Inject] PositionScaleToLocalToWorld positionScaleToLocalToWorld;

struct RotationToLocalToWorld
{
  SubtractiveComponent<Position> Position;
  ComponentDataArray<Rotation> Rotation;
  SubtractiveComponent<Scale> Scale;
  ComponentDataArray<LocalToWorld> LocalToWorld;
}
[Inject] RotationToLocalToWorld rotationToLocalToWorld;

struct RotationScaleToLocalToWorld
{
  SubtractiveComponent<Position> Position;
  ComponentDataArray<Rotation> Rotation;
  ComponentDataArray<Scale> Scale;
  ComponentDataArray<LocalToWorld> LocalToWorld;
}
[Inject] RotationScaleToLocalToWorld rotationScaleToLocalToWorld;

struct ScaleToLocalToWorld
{
  SubtractiveComponent<Position> Position;
  SubtractiveComponent<Rotation> Rotation;
  ComponentDataArray<Scale> Scale;
  ComponentDataArray<LocalToWorld> LocalToWorld;
}
[Inject] ScaleToLocalToWorld scaleToLocalToWorld;
```

`ComponentGroup` is a utility which simplifies iteration over same component type values independent of the `Archetypes` those components belong to. `ComponentGroup` accomplishes this by constraining the types of `Archetypes` that are queried: Either components which must exist in all matching `Archetypes` or components which exist in none of the matching `Archetypes` (subtractive). By contrast, `Chunks` can be iterated in a way that matches how the data is layed out in memory without those same constraints. However, this is at the cost of foregoing `ComponentGroup`, automatic injection and other associated utilities.

Direct `Chunk` iteration allows for "optional" components or managing component combinations more directly.

Another alternative might be to use `ComponentDataFromEntity` and check for the existence of the components on a per-`Entity` basis, as in:
```
[Inject] [ReadOnly] ComponentDataFromEntity<Position> positions;
[Inject] [ReadOnly] ComponentDataFromEntity<Rotation> rotations;
[Inject] [ReadOnly] ComponentDataFromEntity<Scale> scales;

struct LocalToWorldGroup
{
  EntityArray Entities;
  ComponentDataArray<LocalToWorld> LocalToWorld;
}
[Inject] LocalToWorldGroup localToWorldGroup;     
```

An advantage of direct `Chunk` iteration is that any branching that needs to be done based on the existence of a particular component type can be done on a per-`Chunk` basis rather than a per-entity basis.

## Querying matching archetypes

Each `Chunk` belongs to a specific `Archetype`. In order to iterate `Chunks`, you must select a set of `Archetypes`. This is an `EntityArchetypeQuery`.

```
public class EntityArchetypeQuery
{
  public ComponentType[] Any;
  public ComponentType[] None;
  public ComponentType[] All;
}
```

An example might look like:
```
var RootLocalToWorldQuery = new EntityArchetypeQuery
{
  Any = new ComponentType[] {typeof(Rotation), typeof(Position), typeof(Scale)}, 
  None = new ComponentType[] {typeof(Frozen), typeof(Parent)},
  All = new ComponentType[] {typeof(LocalToWorld)},
};
```

Which means RootLocalToWorldQuery will request all archetypes that meet the following conditions:
1. `Archetype` has at least one of `Rotation`, `Position`, or `Scale` component type.
2. `Archetype` does not have Frozen or Parent component types.
3. `Archetype` must have `LocalToWorld` component type.

You can resolve the query with a call to `EntityManager.AddMatchingArchetypes(EntityArchetypeQuery query, NativeList<EntityArchetype> foundArchetypes)`

Additional calls to `AddMatchingArchetypes` passing in the results of previous calls to foundArchetypes will append additional results to the `NativeList`. The logical-or of multiple queries.

Be aware that `EntityArchetypeQuery` uses managed arrays so they should not be created per frame. `OnCreate` in a `ComponentSystem` or `JobComponentSystem` is more appropriate.

## Getting array of Chunks

From a `NativeList<EntityArchetype>` you can retrieve the list of `Chunks` in those `Archetypes` with a call to `EntityManager.CreateArchetypeChunkArray(NativeList<EntityArchetype> archetypes, Allocator allocator)` which will return a `NativeArray<ArchetypeChunk>`.

There is also a utility function `EntityManager.CreateArchetypeChunkArray(EntityArchetypeQuery query, Allocator allocator)` which takes a single `EntityArchetypeQuery` directly and will return a NativeArray<ArchetypeChunk>. This Simplifies the case where no logical-or between multiple EntityArchetypeQuery is needed.

The caller is responsible for calling Dispose() on the `NativeArray<ArchetypeChunk>`.

An ArchetypeChunk type, and by extension a `NativeArray<ArchetypeChunk>`, is always read-only. Therefore you should mark [ReadOnly] when used in jobs.

However, you can retrieve the arrays of component data within those `Chunks` as either read-write or read-only, as needed.

## Accessing component data in chunks

To access data within a `Chunk`, a `ChunkComponentType` is required. This represents the specific component type and read-only attribute requested. From within a `ComponentSystem` or `JobComponentSystem`, this is retrieved by a call to `GetArchetypeChunkComponentType<T>(bool isReadOnly = false)` which returns a `ArchetypeChunkComponentType<T>`.

For instance, in order to gain read-only access to the `Position` component data in the `Chunks` matching the `Archetypes` above: 
```
var RotationTypeRO = GetArchetypeChunkComponentType<Rotation>(true);
```
Or read-write access to the `LocalToWorld` component data:
```
var LocalToWorldTypeRW = GetArchetypeChunkComponentType<LocalToWorld>(false);
```

When used in a `Job`, the \[ReadOnly\] attribute must match the type. For example:
```
  [ReadOnly] public ArchetypeChunkComponentType<Rotation> rotationType;
  public ArchetypeChunkComponentType<LocalToWorld> localToWorldType;
```

To retrieve the actual component data for reading or editing, `ArchetypeChunk.GetNativeSlice<T>(ArchetypeChunkComponentType<T> chunkComponentType)` is used, which returns `NativeSlice<T>`

For example, for a given `ArchetypeChunk` (`Chunk`), you can retrieve the `Position` and `LocalToWorld` data as:
```
var chunkPositions = chunk.GetNativeSlice(positionType);
var chunkLocalToWorlds = chunk.GetNativeSlice(localToWorldType);
```

Implicit to an `EntityArchetypeQuery` is that every `Chunk` may not have the same components available. In this case, for instance, `Chunks` coming from different `archetypes` may or may not have `Position` components. In that case, the length of the returned array will be zero. For example, you can confirm the existence of a `Position` component data in a `Chunk` by:
```
var chunkPositionsExist = chunkPositions.Length > 0;
```

For iteration, you can retrieve the number of instances in a `Chunk` with `ArchetypeChunk.Count`.

After confirming existence of the component data for the `Chunk`, you can read/write the data as expected. For example:
```
for (int i = 0; i < chunk.Count; i++)
{
  chunkLocalToWorlds[i] = new LocalToWorld
  {
    Value = float4x4.translate(chunkPositions[i].Value)
  };
}
```

## Accessing entity data in chunks

Iterating `Entity` values in `Chunks` is very similar to components.

The `Entity` type is requested from within a `ComponentSystem` or `JobComponentSystem` by `GetArchetypeChunkEntityType()` with returns a `ArchetypeChunkEntityType`. `Entity` type is always read-only.

For example:
```
var EntityTypeRO = GetArchetypeChunkEntityType();
```

Similarly `ArchetypeChunkEntityType` should always include the \[ReadOnly\] attribute when used in a `Job`. For example:.
```
[ReadOnly] public ArchetypeChunkEntityType entityType;
```

To retrieve the `Entity` values given a specific `ArchetypeChunk`, `GetNativeSlice` is used as with component data: 
```
var chunkEntities = chunk.GetNativeSlice(entityType);
```

## Accessing SharedComponent (index) data in chunks

You cannot directly access `SharedComponent` in a `Chunk` because `SharedComponent` data is not stored in the `Chunks`. However, each `archetype` contains the index of the specific value of the `SharedComponent` which is part of its definition, and indexes into the global list of `SharedComponent` values.

Retrieving the `SharedComponent` index works very much like retrieving component data. An `ArchetypeChunkSharedComponentType<T>` is returned by a call to `GetArchetypeChunkSharedComponentType<T>()` within a `ComponentSystem` or `JobComponentSystem`. 

Like `ArchetypeChunkEntityType`, `ArchetypeChunkSharedComponentType` is always read-only.

`ArchetypeChunk.GetSharedComponentIndex<T>(ArchetypeChunkSharedComponentType<T> chunkSharedComponentData)` returns the index of the `SharedComponent`.

For example:
```
var chunkDepthSharedIndex = chunk.GetSharedComponentIndex(depthType);
```

Where depthType is:
```
[ReadOnly] public ArchetypeChunkSharedComponentType<Depth> depthType;
```

You can retrieve the total number of `SharedComponent` instances the archetype  the `Chunk` belongs to includes by: `ArchetypeChunk.NumSharedComponents()`

## Accessing SharedComponent data from ArchetypeChunk.GetSharedComponentIndex

You can retrieve `SharedComponent` values from EntityManager via `EntityManager.GetAllUniqueSharedComponentData<T>(List<T> sharedComponentValues)`. However, this list is per `SharedComponent` type. The indices returned by `ArchetypeChunk.GetSharedComponentIndex` refer to the global `SharedComponent` list and are not per-type.

In order to resolve these indices, a mapping from global to per-type index is needed.

You can retrieve both the `SharedComponent` values and the remapping from `EntityManager` via: `EntityManager.GetAllUniqueSharedComponentData<T>(List<T> sharedComponentValues, List<int> sharedComponentIndices)` 

For each `sharedComponentValue[i]`, the `sharedComponentIndices[i]` stores the global index of the `SharedComponent` value.

For example:
```
var sharedDepths = new List<Depth>();
var sharedDepthIndices = new List<int>();
EntityManager.GetAllUniqueSharedComponentData(sharedDepths, sharedDepthIndices);

...

var chunkDepthSharedIndex = chunk.GetSharedComponentIndex(depthType);
var chunkDepthIndex = sharedDepthIndices.IndexOf(chunkDepthSharedIndex);
var chunkDepth = sharedDepths[chunkDepthIndex];
```

Additionally, you can retrieve the total count of all `SharedComponents` in the global list with: `EntityManager.GetSharedComponentCount()`

## Common use in Jobs

It is expected to iterate over `NativeArray<ArchetypeChunk>` in an `IJobParallelFor` (see `JobParallelFor`). Then within the `Job` `Execute`, for each `ArchetypeChunk`, based on what components exist, the code would loop over all the appropriate components. It is a batch operation.

Additionally:

- `ArchetypeChunkArray.CalculateEntityCount(NativeArray<ArchetypeChunk> chunks)` is a utility which returns the complete `Entity` count for all `Chunks` in the array.
- Each `ArchetypeChunk` contains a `StartIndex` value which is the `Entity` count offset within the NativeArray<ArchetypeChunk>.

For example:
```
struct CollectValues : IJobParallelFor
{
    [ReadOnly] public NativeArray<ArchetypeChunk> chunks;
    [ReadOnly] public ArchetypeChunkComponentType<EcsTestData> ecsTestData;

    [NativeDisableParallelForRestriction] public NativeArray<int> values;

    public void Execute(int chunkIndex)
    {
        var chunk = chunks[chunkIndex];
        var chunkStartIndex = chunk.StartIndex;
        var chunkCount = chunk.Count;
        var chunkEcsTestData = chunk.GetNativeSlice(ecsTestData);

        for (int i = 0; i < chunkCount; i++)
        {
            values[chunkStartIndex + i] = chunkEcsTestData[i].value;
        }
    }
}

public void TestCollectValues()
{
    var query = new EntityArchetypeQuery
    {
        Any = Array.Empty<ComponentType>(),
        None = Array.Empty<ComponentType>(),
        All = new ComponentType[] {typeof(EcsTestData)}
    };
    var chunks = m_Manager.CreateArchetypeChunkArray(query, Allocator.Temp);
    var ecsTestData = m_Manager.GetArchetypeChunkComponentType<EcsTestData>(true);
    var entityCount = ArchetypeChunkArray.CalculateEntityCount(chunks);
    var values = new NativeArray<int>(entityCount, Allocator.TempJob);
    var collectValuesJob = new CollectValues
    {
        chunks = chunks,
        ecsTestData = ecsTestData,
        values = values
    };
    var collectValuesJobHandle = collectValuesJob.Schedule(chunks.Length, 64);
    collectValuesJobHandle.Complete();
    chunks.Dispose();

    // Use values here...

    values.Dispose();
}
```

## Change Versions

When a type is changed within a `Chunk`, the version for that type within the `Chunk` is assigned to the `EntityManager.GlobalSystemVersion`. By comparing the version number of a type within a `Chunk` to the current `GlobalSystemVersion` in a `ComponentSystem` or `JobComponentSystem`, you can infer whether or not the type values have changed.

Special value: If the version number is zero, the `Chunk` is new.

The version number is returned by `ArchetypeChunk.GetComponentVersion<T>(ArchetypeChunkComponentType<T> chunkComponentType)` For example `chunk.GetComponentVersion(positionType)`

Utilities are provided to compare version numbers and determine change:

Using `ChangeVersionUtility.DidChange(uint changeVersion, uint requiredVersion)`, passing in the `Chunk`ype version number and the expected system version respectively, will return whether or not the specified type in the `Chunk` has been changed. New `Chunks` will return false.

`ChangeVersionUtility.DidAddOrChange(uint changeVersion, uint requiredVersion)` given the `Chunk` type version number and the expected system version, respectively, will return whether or not the specified type in the `Chunk` has been changed or is new.

By way of example, in this case a `Chunk` is skipped if no change was made to the specified types since the last iteration:
```
var chunkRotationsChanged = ChangeVersionUtility.DidAddOrChange(chunk.GetComponentVersion(rotationType), lastSystemVersion);
var chunkPositionsChanged = ChangeVersionUtility.DidAddOrChange(chunk.GetComponentVersion(positionType), lastSystemVersion);
var chunkScalesChanged = ChangeVersionUtility.DidAddOrChange(chunk.GetComponentVersion(scaleType), lastSystemVersion);
var chunkAnyChanged = chunkRotationsChanged || chunkPositionsChanged || chunkScalesChanged;

if (!chunkAnyChanged)
  return;
```

[Back to Capsicum reference](index.md)