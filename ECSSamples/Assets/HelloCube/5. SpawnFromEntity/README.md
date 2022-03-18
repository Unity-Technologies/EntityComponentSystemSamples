# HelloCube: SpawnFromEntity

This sample demonstrates a different way to spawn Entities and Components using a Prefab GameObject. Like the previous example, the scene spawns a "field" of the pairs of spinning cubes.

## What does it show?

There are two key differences to the way Entities were spawned in the previous example:

1. In this example, the Prefab GameObject is converted to an Entity representation by the SpawnerAuthoring_FromEntity MonoBehaviour, in its IConvertGameObjectToEntity.Convert method.

2. The example uses a JobSystem to spawn the Entities rather than a MonoBehaviour Start() method.

## Spawning entities

Component data can have references to other entities. In this case we have a reference to a entity prefab. Similar to GameObjects, when instantiating and destroying a prefab, the whole prefab is cloned or deleted as a group.

This way you can write all of your runtime code using a System and ComponentData.

The SpawnerSystem_FromEntity looks for any Spawner_FromEntity Component.
When it finds one, the System instantiates the prefabs in a grid and then destroys the spawner Entity (so that it only spawns a given set of Entities once).

The SpawnerSystem_FromEntity uses an Entities.ForEach, which is very similar to the those demonstrated in earlier examples. The difference is that this Entities.ForEach provides the Entity object (and the entity index in query) to your lambda. The Entity object is required so that the System can destroy the Spawner Entity once it has been processed.

Currently, the most efficient way to batch create a large number of entities is to batch-instantiate all copies of the prefab using EntityManager.Instantiate(), and then use a parallel job to provide the initial per-instance component values.