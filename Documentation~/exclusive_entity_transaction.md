# ExclusiveEntityTransaction

`ExclusiveEntityTransaction` is an API to create & destroy entities from a job. The purpose is to enable procedural generation scenarios where instantiation on big scale must happen on jobs. As the name implies it is exclusive to any other access to the [EntityManager](entity_manager.md).

`ExclusiveEntityTransaction` should be used on a manually created [World](world.md) that acts as a staging area to construct & setup entities.

After the job has completed you can end the `ExclusiveEntityTransaction` and use ```EntityManager.MoveEntitiesFrom(EntityManager srcEntities);``` to move the entities to an active `World`.

[Back to Unity Data-Oriented reference](reference.md)