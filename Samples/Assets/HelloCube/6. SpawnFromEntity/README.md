# HelloCube: SpawnFromEntity

This sample demonstrates a different way to spawn Entities and Components using a Prefab GameObject. Like the previous example, the scene spawns a "field" of the pairs of spinning cubes.

## What does it show?

There are two key differences to the way Entities were spawned in the previous example:

1. In this example, the Prefab GameObject is converted to an Entity representation by the SpawnerAuthoring_FromEntity MonoBehaviour, in its IConvertGameObjectToEntity.Convert method.

2. The example uses a JobSystem to spawn the Entities rather than a MonoBehaviour Start() method.

## Spawning entities

Component data can have references to other entities. In this case we have a reference to a entity prefab. Similar to GameObjects, when instantiating and destroying a prefab, the whole prefab is cloned or deleted as a group.

This way you can write all of your runtime code using JobComponentSystem and ComponentData.

The SpawnerSystem_FromEntity looks for any Spawner_FromEntity Component.
When it finds one, the System instantiates the prefabs in a grid and then destroys the spawner Entity (so that it only spawns a given set of Entities once).

The SpawnerSystem_FromEntity uses a Job based on IJobForEachWithEntity, which is very similar to the IJobForEach Jobs demonstrated in earlier examples. The difference is that this type of Job provides the Entity object (and the Component array index) to your Execute() function. The Entity object is required so that the System can destroy the Spawner Entity once it has been processed.

Another important concept that SpawnerSystem_FromEntity illustrates is the use of an EntityCommandBuffer and a EntityCommandBufferSystem. To prevent race conditions, you cannot make _structural changes_ inside a Job. Structural changes include anything that changes the structure of your data, such as creating/destroying Entities or adding/removing Components. To overcome this limitation, ECS provides the EntityCommandBuffer.

Instead of performing structural changes directly, a Job can add a command to an EntityCommandBuffer to perform such changes on the main thread after the Job has finished. Command buffers allow you to perform any, potentially costly, calculations on a worker thread, while queuing up the actual insertions and deletions for later.

The EndSimulationBarrier is a standard ECS System that provides an EntityCommandBuffer for any System to use. EndSimulationBarrier automatically executes any commands in this buffer when the System runs at the end of a frame.
