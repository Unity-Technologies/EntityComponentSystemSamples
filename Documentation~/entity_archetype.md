# EntityArchetype

An `EntityArchetype` is a unique array of `ComponentType`. `EntityManager` uses `EntityArchetype` stucts to group all entities using the same component types in [Chunks](chunk_iteration.md).

```C#
// Using typeof to create an EntityArchetype from a set of components
EntityArchetype archetype = EntityManager.CreateArchetype(typeof(MyComponentData), typeof(MySharedComponent));

// Same API but slightly more efficient
EntityArchetype archetype = EntityManager.CreateArchetype(ComponentType.Create<MyComponentData>(), ComponentType.Create<MySharedComponent>());

// Create an Entity from an EntityArchetype
var entity = EntityManager.CreateEntity(archetype);

// Implicitly create an EntityArchetype for convenience
var entity = EntityManager.CreateEntity(typeof(MyComponentData), typeof(MySharedComponent));

```

[Back to Unity Data-Oriented reference](reference.md)