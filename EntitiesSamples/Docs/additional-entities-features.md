
In this page: 

- [Enableable components](#enableable-components)
- [Shared components](#shared-components)
- [Cleanup components](#cleanup-components)
- [Chunk components](#chunk-components)
- [Blob assets](#blob-assets)
- [Version numbers](#version-numbers)

<br>

# Enableable components

A struct implementing `IComponentData` or `IBufferElementData` can also implement [`IEnableableComponent`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.IEnableableComponent.html). A component type implementing this interface can be enabled and disabled per entity.

**When a component of an entity is disabled, queries consider the entity to not have the component type.** If no entities in a chunk match the query because one or more of their components are disabled, that chunk will not be included in the array returned by the [`ToArchetypeChunkArray()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityQuery.ToArchetypeChunkArray.html) method of `EntityQuery`.

Be clear that disabling a component does *not* remove or modify the component: rather, a bit associated with the specific component of the specific entity is cleared. Also be clear that a disabled component only affects queries: a disabled component can otherwise still be read and modified as normal, such as *via* `EntityManager` methods.

All enableable components are enabled by default on a newly created entity. When an entity is copied for serialization, copied to another world, or copied by the `Instantiate` method of `EntityManager`, the enabled states of the components in the new entity match the states in the original.

The enabled state of an entity's components can be checked and set through:

- [`EntityManager`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityManager.html)
- [`ComponentLookup<T>`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.ComponentLookup-1.html)
- [`BufferLookup<T>`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.BufferLookup-1.html) (for a dynamic buffer)
- [`EnabledRefRW<T>`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EnabledRefRW-1.html) (used in a `SystemAPI.Query` foreach or an `IJobEntity`)
- [`ArchetypeChunk`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.ArchetypeChunk.html)

For instance, the `EntityManager` has these key methods:

|**Method**|**Description**|
|----|---|
| [`IsComponentEnabled<T>()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityManager.IsComponentEnabled.html) | Returns true if an entity has a currently enabled T component. |
| [`SetComponentEnabled<T>()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityManager.SetComponentEnabled.html) | Enables or disables an entity's enableable T component. |

| &#x1F4DD; NOTE |
| :- |
| For the sake of the job safety checks, read or write access of a component's enabled state requires read or write access of the component type itself. |

In an `IJobChunk`, the `Execute` method parameters signal which entities in the chunk match the query:

- If the `useEnableMask` parameter is false, all entities in the chunk match the query. 
- Otherwise, if the `useEnableMask` parameter is true, the bits of the `chunkEnabledMask` parameter signal which entities in the chunk match the query, factoring in all enableable component types of the query. Rather than check these mask bits manually, you can use a [`ChunkEntityEnumerator`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.ChunkEntityEnumerator.html) to more conveniently iterate through the matching entities.

| &#x1F4DD; NOTE |
| :- |
| The `chunkEnabledMask` is a *composite* of all the enabled states of the enableable components included in the query of the job. To check enabled states of individual components, use the `IsComponentEnabled()` and `SetComponentEnabled()` methods of the `ArchetypeChunk`. |

<br>

# Shared components

For a shared component type, all entities in a chunk share the same component value rather than each entity having its own value. Consequently, **setting a shared component value of an entity performs a [structural change](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/concepts-structural-changes.html)**: the entity is moved to a chunk which has the new value.

For example, if an entity has a *Foo* shared component value *X*, then the entity is stored in a chunk that has *Foo* value *X*; if the entity is then set to have *Foo* value *Y*, the entity is moved to a chunk that has value *Y*; if no such chunk already exists, a new chunk is created. 

The primary utility of shared components comes from the fact that **queries can filter for specific shared component values**.

Instead of storing shared component values directly in chunks, the world stores them in a set of arrays, and the chunks store just indexes into these arrays. This means that **each unique shared component value is stored only once within a world**.

A shared component type is declared as a struct implementing `ISharedComponentData`. If the struct contains any managed type fields, then the shared component will itself be a managed component type, with the same advantages and restrictions as a managed `IComponentData`.

The `EntityManager` has these key methods for shared components:

|**Method**|**Description**|
|----|---|
| [`AddComponent<T>()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityManager.AddComponent.html) | Adds a T component to an entity, where T can be a shared component type. |
| [`AddSharedComponent()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityManager.AddSharedComponent.html) | Adds an unmanaged shared component to an entity and sets its initial value. |
| [`AddSharedComponentManaged()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityManager.AddSharedComponentManaged.html) | Adds a managed shared component to an entity and sets its initial value. |
| [`RemoveComponent<T>()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityManager.RemoveComponent.html) | Removes a T component from an entity, where T can be a shared component type. |
| [`HasComponent<T>()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityManager.HasComponent.html) | Returns true if an entity currently has a T component, where type T can be a shared component type. |
| [`GetSharedComponent<T>()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityManager.GetSharedComponent.html) | Retrieves the value of an entity's unmanaged shared T component. |
| [`SetSharedComponent<T>()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityManager.SetSharedComponent.html) | Overwrites the value of an entity's unmanaged shared T component. |
| [`GetSharedComponentManaged<T>()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityManager.GetSharedComponentManaged.html) | Retrieves the value of an entity's managed shared T component. |
| [`SetSharedComponentManaged<T>()`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityManager.SetSharedComponentManaged.html) | Overwrites the value of an entity's managed shared T component. |

How a shared component type is compared for equality by the `EntityManager` can be customized by implementing [`IEquatable<T>`](https://docs.microsoft.com/en-us/dotnet/api/system.iequatable-1.equals).

| &#x26A0; IMPORTANT |
| :- |
| Because the `EntityManager` relies upon equality to identify unique and matching shared component values, you should avoid modifying any mutable objects referenced by shared components. For example, if you want to modify an array stored in a shared component of a particular entity, you should not modify the array directly but instead update the component of that entity to have a new, modified copy of the array. |

If a shared component type implements `IRefCounted`, you can use reference counting to detect when a value of the type is no longer stored by any world. For example, if a shared component value that implements `IRefCounted` contains a `NativeArray`, you can dispose the array when the value is no longer stored by any world.

If the shared component type is unmanaged, the methods of `IEquatable<T>` and `IRefCounted` can be Burst-compiled by adding the [`[BurstCompile]`](https://docs.unity3d.com/Packages/com.unity.burst@latest/index.html?subfolder=/api/Unity.Burst.BurstCompileAttribute.html) attribute to the methods and the struct itself.


| &#x26A0; IMPORTANT |
| :- |
| **Having too many unique shared component values may result in chunk fragmentation.** <br> Because all entities in a chunk must share the same shared component values, if you give unique shared component values to a high number of entities, the entities will end up fragmented across many chunks. For example, if there are 500 entities of an archetype with a shared component and each entity has a unique shared component value, each entity is stored by itself in a separate chunk. This wastes most of the space in each chunk and also means that looping through all entities of the archetype requires visiting 500 chunks. This fragmentation largely negates the performance benefits of the ECS structure. To avoid this problem, try to use as few unique shared component values as possible. If, say, the 500 entities were to share only ten unique shared component values, they could be stored in as few as ten chunks. |

<br>

# Cleanup components

Cleanup components are special in two ways:

- When an entity with cleanup components is destroyed, the non-cleanup components are removed, but the entity actually continues to exist until you remove all of its cleanup components individually.
- When an entity is copied to another world, copied in serialization, or copied by the `Instantiate` method of `EntityManager`, any cleanup components of the original are *not* added to the new entity.

The primary use case for cleanup components is to help initialize entities after their creation or cleanup entities after their destruction. For example, say we have entities representing monsters, and they all have a *Monster* tag component:

1. We can find all monster entities needing initialization by querying for all entities which have the *Monster* component but which do *not* have a *MonsterCleanup* component. For all entities matching this query, we perform any required initialization and add *MonsterCleanup*.
2. We can find all monster entities needing cleanup by querying for all entities which have the *MonsterCleanup* component but *not* the *Monster* component. For all entities matching this query, we perform any required cleanup and remove *MonsterCleanup*. Unless the entities have additional remaining cleanup components, this will destroy the entities.

| &#x1F4DD; NOTE |
| :- |
| In some case, you'll want to store information needed for cleanup in your cleanup components, but in many cases, an empty cleanup tag component is sufficient. |

Cleanup components come in four varieties:

|**Kind of cleanup component**|**Description**|
|---|---|
| A struct implementing `ICleanupComponentData` | The cleanup variant of an unmanaged `IComponentData` type.|
| A class implementing `ICleanupComponentData` | The cleanup variant of a managed `IComponentData` type.|
| A struct implementing `ICleanupBufferElementData` | The cleanup variant of a dynamic buffer type.|
| A struct implementing `ICleanupSharedComponentData` | The cleanup variant of a shared component type.|

<br>

# Chunk components

Unlike a regular component, a chunk component is a single value belonging to the whole chunk, not any entity within the chunk.

Just like a regular component, a chunk component is defined as a struct or class implementing `IComponentData`, but a chunk component is added, removed, get, and set with these `EntityManager` methods:

|**Method**|**Description**|
|----|---|
| [`AddChunkComponentData<T>`](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/api/Unity.Entities.EntityManager.AddChunkComponentData.html) | Adds a chunk component of type T to a chunk, where T is a managed or unmanaged `IComponentData`. |
| [`RemoveChunkComponentData<T>`](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/api/Unity.Entities.EntityManager.RemoveChunkComponentData.html) | Removes a chunk component of type T from a chunk, where T is a managed or unmanaged `IComponentData`. |
| [`HasChunkComponent<T>`](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/api/Unity.Entities.EntityManager.HasChunkComponent.html) | Returns true if a chunk has a chunk component of type T. |
| [`GetChunkComponentData<T>`](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/api/Unity.Entities.EntityManager.GetChunkComponentData.html) | Retrieves the value of a chunk's chunk component of type T. |
| [`SetChunkComponentData<T>`](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/api/Unity.Entities.EntityManager.SetChunkComponentData.html) | Sets the value of a chunk's chunk component of type T. |

| &#x1F4DD; NOTE |
| :- |
| Shared components also store one value per chunk, but a shared component value logically belongs to the entities, not the chunk (which is why setting an entity's shared component value moves the entity to another chunk rather than modifying the value stored in the chunk). Chunk components truly belong to the chunk itself, and unlike unmanaged shared components, unmanaged chunk components are stored directly in the chunk.|

<br>

# Blob assets

A Blob (Binary Large Object) asset is an immutable (unchanging), unmanaged piece of binary data stored in a contiguous block of bytes:

- Blob assets are efficient to copy and load because they are fully *relocatable*: all internal pointers are expressed as relative offsets instead of absolute addresses, so copying the whole Blob is as simple as copying every byte.
- Although they are stored independently from entities, Blob assets may be _referenced_ from entity components.
- Because they're immutable, Blob assets are inherently safe to access from multiple threads.

| &#x1F4DD; NOTE |
| :- |
| The name Blob "asset" is a bit misleading: a Blob asset is a piece of data in memory, not a project asset file! However, Blob assets are efficiently and easily serializable into files on disk, so it makes some sense to call them "assets". |

To create a Blob asset:

1. Create a [BlobBuilder](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/api/Unity.Entities.BlobBuilder.html).
1. Call the builder's [`ConstructRoot<T>`](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/api/Unity.Entities.BlobBuilder.ConstructRoot.html) to set the Blob's 'root' (a struct of type T).
1. Call the builder's [`Allocate<T>`](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/api/Unity.Entities.BlobBuilder.Allocate.html), [`Construct<T>`](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/api/Unity.Entities.BlobBuilder.Construct.html) and [`SetPointer<T>`](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/api/Unity.Entities.BlobBuilder.SetPointer.html) methods to fill in the rest of the Blob data (including [`BlobArray`](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/api/Unity.Entities.BlobArray-1.html)'s, [`BlobString`](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/api/Unity.Entities.BlobString.html)'s, and [`BlobPtr`](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/api/Unity.Entities.BlobPtr-1.html)).
1. Call the builder's [CreateBlobAssetReference](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/api/Unity.Entities.BlobBuilder.CreateBlobAssetReference.html), which copies all the data in the builder to create the actual Blob asset and returns a [BlobAssetReference](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/api/Unity.Entities.BlobAssetReference-1.html).
1. Dispose the `BlobBuilder`.

When a Blob asset is no longer needed, it should be disposed by calling `Dispose` on the `BlobAssetReference`.

Blob assets referenced in a baked entity scene are serialized and loaded along with the scene. These Blob assets should *not* be manually disposed: they will be automatically disposed along with the scene.

| &#x26A0; IMPORTANT |
| :- |
| All parts of a blob asset that contain internal pointers must always be accessed by reference. For example, the offset values in a BlobString struct are only correct relative to where the BlobString struct is stored inside the Blob; the offsets are not correct relative to *copies* of the struct. |

<br>

# Version numbers

A world, its systems, and its chunks maintain several **'version numbers'** (numbers which are incremented by certain operations). By comparing version numbers, you can determine if certain data might have changed.

All version numbers are 32-bit signed integers, so when incremented, they eventually wrap around. The proper way to compare version numbers then relies upon subtle quirks of how C# defines signed integer overflow:

```csharp
// true if VersionB is more recent than VersionA
// false if VersionB is equal or less than VersionA
bool changed = (VersionB - VersionA) > 0;
```

|**Version number**|**Description**|
|---|---|
| [`World.Version`](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/api/Unity.Entities.World.Version.html) | Increased every time the world adds or removes a system or system group. |
| [`EntityManager.GlobalSystemVersion`](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/api/Unity.Entities.EntityManager.GlobalSystemVersion.html) | Increased before every system update in the world. |
| [`SystemState.LastSystemVersion`](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/api/Unity.Entities.SystemState.LastSystemVersion.html) | Assigned the value of the `GlobalSystemVersion` immediately after each time the system updates. |
| [`EntityManager.EntityOrderVersion`](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/api/Unity.Entities.EntityManager.EntityOrderVersion.html) | Increased every time a structural change is made in the world. |

Each component type has its own version number, which is incremented by any operation that gets write access to the component type. This number can be retrieved by calling the method [`EntityManager.GetComponentOrderVersion`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.EntityManager.GetComponentOrderVersion.html).

Each shared component *value* also has a version number that is increased every time a structural change affects a chunk having the value.

A chunk stores a version number for each component type in the chunk. When a component type in a chunk is accessed for writing, its version number is assigned the value of `EntityManager.GlobalSystemVersion`, regardless of whether any component values are actually modified. These chunk version numbers can be retrieved by calling the [`ArchetypeChunk.GetChangeVersion`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.ArchetypeChunk.GetChangeVersion.html) method.

A chunk also stores a version number for each component type which is assigned the value of `EntityManager.GlobalSystemVersion` every time a structural change affects the chunk. These chunk version numbers can be retrieved by calling the [`ArchetypeChunk.GetOrderVersion`](https://docs.unity3d.com/Packages/com.unity.entities@latest?subfolder=/api/Unity.Entities.ArchetypeChunk.GetOrderVersion.html) method.

<br>
