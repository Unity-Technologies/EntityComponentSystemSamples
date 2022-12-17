# Entities and Components

An **entity** is a lightweight, unmanaged alternative to a [GameObject](https://docs.unity3d.com/Manual/class-GameObject.html): 

- Unlike a GameObject, an entity is not a managed object but simply a unique identifier number.
- The **components** associated with an entity are usually struct values.
- A single entity can only have one component of any given type. For example, a single entity cannot have two components both of type *Foo*.
- Although entity component types can be given methods, doing so is generally discouraged.
- An entity has no built-in concept of parenting. Instead, the standard `Parent` component contains a reference to another entity, allowing formation of entity transform hierarchies.

Entity component types are defined by implementing these interfaces:

|**Kind of component**|**Description**|
|---|---|
| [`IComponentData`](components.md) | Defines the most common, basic kind of component type.|
| [`IBufferElementData`](components-buffer.md) | Defines a dynamic buffer (growable array) component type.|
| [`ISharedComponent`](components-shared.md) | Defines a shared component type, whose values can be shared by multiple entities.|
| [`ICleanupComponent`](components-cleanup.md)  | Defines a cleanup component type, which facilitates proper setup and teardown of resources.|

There are also two additional interfaces ([`ICleanupSharedComponent`]() and [`ICleanupBufferElementData`]()) and [chunk components](components-chunk.md) (which are defined with `IComponentData` but added and removed from entities by a distinct set of methods).

A component type defined with `IComponentData` or `IBufferElementData` can be made **'enableable'** by also implementing [`IEnableableComponent`](components-enableable.md).

An `IComponentData` component type which contains no data is known as a *tag component*. Despite storing no data, adding a tag component to an entity can be used to mark the entity. For example, you might add a *Monster* component to all of your entities which represent monsters, and then you can effectively query for all monster entities.

<br>

## Worlds and EntityManagers

A [`World`]() is a collection of entities. An entity's ID number is only unique within its own world, *i.e.* the entity with a particular ID in one world is entirely unrelated to the entity with the same ID in a different world.

A world also owns a set of [systems](concepts-systems.md), which are units of code that run on the main thread, usually once per frame. The entities of a world are normally only accessed by the world's systems (and the jobs scheduled by those systems), but this is not an enforced restriction.

The entities in a world are created, destroyed, and modified through the world's [`EntityManager`](xref:Unity.Entities.EntityManager). Key `EntityManager` methods include:

|**Method**|**Description**|
|---|---|
| [`CreateEntity()`]() | Creates a new entity. |
| [`Instantiate()`]() | Creates a new entity with a copy of all the components of an existing entity. |
| [`DestroyEntity()`]() | Destroys an existing entity. |
| [`AddComponent<T>()`]() | Adds a component of type T to an existing entity. |
| [`RemoveComponent<T>()`]() | Removes a component of type T from an existing entity. |
| [`HasComponent<T>()`]() | Returns true if an entity currently has a component of type T. |
| [`GetComponent<T>()`]() | Retrieves the value of an entity's component of type T. |
| [`SetComponent<T>()`]() | Overwrites the value of an entity's component of type T. |

| &#x1F4DD; NOTE |
| :- |
| All of the above methods, except `GetComponent` and `SetComponent`, are [structural change](concepts-structural-changes.md) operations. |

## Archetypes

An **archetype** represents a particular combination of component types in a world: all of the entities in a world with a certain set of component types are stored together in the same archetype. For example:

 - All of a world's entities with component types *A*, *B*, and *C*, are stored together in one archetype,
 - ...the entities with component types *A* and *B* (but not *C*) are stored together in a second archetype,
 - ...and the entities with component types *B* and *D* are stored in a third archetype.

Effectively, adding or removing components of an entity changes which archetype the entity belongs to, necessitating the `EntityManager` to actually move the entity and its components from its old archetype to its new one.

When you add or remove components from an entity, the  `EntityManager` moves the entity to the appropriate archetype. For example, if an entity has component types *X*, *Y*, and *Z* and you remove its *Y* component, the `EntityManager` moves the entity to the archetype that has component types *X* and *Z*. If no such archetype already exists in the world, the `EntityManager` creates it.

Even if all the entities are removed from an archetype, the archetype is only destroyed when its world is destroyed.

| &#x26A0; IMPORTANT |
| :- |
| Moving too many entities between archetypes too frequently can add up to significant costs. See the documentation on  [structural changes](concepts-structural-changes.md). |


## Chunks

The entities of an archetype are stored in 16KiB blocks of memory belonging to the archetype called *chunks*. Each chunk stores up to 128 entities, with the precise number depending upon the number and size of the archetype's component types.

The entity ID's and components of each type are stored in their own separate array within the chunk. For example, in the archetype for entities which have component types *A* and *B*, each chunk will store three arrays: one array for the entity ID's, a second array for the *A* components, and a third array for the *B* components.

The ID and components of the first entity in the chunk are stored at index 0 of these arrays, the second entity at index 1, the third entity at index 2, and so forth.

A chunk's arrays are always kept tightly packed:

- When a new entity is added to the chunk, it is stored in the first free index of the arrays.
- When an entity is removed from the chunk (which happens either because the entity is being destroyed or because it's being moved to another archetype), the last entity in the chunk is moved to fill in the gap.

The creation and destruction of chunks is handled by the `EntityManager`:

- The `EntityManager` creates a new chunk only when an entity is added to an archetype whose already existing chunks are all full.
- The `EntityManager` only destroys a chunk when the chunk's last entity is removed.

## Entity ID's

An entity ID is represented by the struct [`Entity`]().

In order to look up entities by ID, the world’s `EntityManager` maintains an array of entity metadata. Each entity ID has an index value denoting a slot in this metadata array, and the slot stores a pointer to the chunk where that entity is stored, as well as the index of the entity within the chunk. When no entity exists for a particular index, the chunk pointer at that index is null. Here, for example, no entities with indexes 1, 2, and 5 currently exist, so the chunk pointers in those slots are all null.

![entity metadata](http://url/to/img.png)

In order to allow entity indexes to be reused after an entity is destroyed, each entity ID also contains a version number. When an entity is destroyed, the version number stored at its index is incremented, and so if an ID’s version number doesn’t match the one currently stored, then the ID must refer to an entity that has already been destroyed or perhaps never existed.

![entity metadata version numbers](http://url/to/img.png)

## Queries

An `EntityQuery` efficiently finds all entities having a specified set of component types. For example, if a query looks for all entities having component types *A* and *B*, then the query will gather the chunks of all archetypes having those two component types, regardless of whatever other component types those archetypes might have. So such a query would match the archetype with component types *A* and *B*, but the query would also match the archetype with component types  *A*, *B*, and *C*.

A query can specify component types to *exclude* from the matching archetypes. For example, if a query looks for all entities having component types *A* and *B* but *not* having component type *C*, the query would match the archetype with component types *A* and *B*, but the query would *not* match the archetype with component types *A*, *B*, and *C*.

[todo "Any" of a query]

A [change filter]() on a query filters the matching chunks based on whether the values of certain component types may have changed since a prior time.

A [shared component filter]() on a query filters the matching chunks sharing a certain shared component value. 

*[Examples of creating and using queries]().*

| &#x1F4DD; NOTE |
| :- |
| The archetypes matching a query will get cached until the next time a new archetype is added to the world. Because the set of existing archetypes in a world tends to stabilize early in the lifetime of a program, this caching tends to improve performance. |