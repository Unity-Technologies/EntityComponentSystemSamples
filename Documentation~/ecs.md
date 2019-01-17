# ECS

A general computing term that is also used in Unity.

An [entity-component-system](https://en.wikipedia.org/wiki/Entity%E2%80%93component%E2%80%93system) (ECS) is a new model to write performant code by default. Instead of using [Object-Oriented Design](https://en.wikipedia.org/wiki/Object-oriented_design) (OOD), ECS takes advantage of another paradigm called [Data-Oriented Design](https://en.wikipedia.org/wiki/Data-oriented_design). This  separates out the data from the logic so you can apply instructions to a large batch of items in parallel. The Entity-component-system gurantees [linear data layout](https://en.wikipedia.org/wiki/Flat_memory_model) when iterating over entities in [chunks](chunk_iteration.md). Managing data this way is quicker because you read from continuous blocks of memory, rather than random blocks assigned all over the place. Knowing exactly where each bit of data is, and by packing it tightly together, allows us to manage memory with little overhead. This is a critical part of the performance gains provided by ECS.

>  Note: Unity's ECS is a fairly standard entity-component-system, although the naming is tweaked somewhat to avoid clashes with existing concepts within Unity. (See [ECS concepts](ecs_concepts.md) for more information.)

See also: [Entity](entity.md), [ComponentData](component_data.md), and [ComponentSystem](component_system.md). 

[Back to Unity Data-Oriented reference](reference.md)