# HelloCube: SpawnAndRemove

This sample demonstrates spawning and removing Entities from the World.

## What does it show?

1. Two different methods of adding and removing Entities from the World
    - From data
    - From code (by manually loading the resource and creating the Entity)

2. The performance characteristics of adding and removing Entities from the World does not change due to fragmentation of chunks.

3. Chunk utilization widget histogram activity in the EntityDebugger.

When the example starts, Entities are added into the World via a SpawnJob in the same way as the HelloCube SpawnFromEntity sample demonstrates. In this example, each red cube is an Entity that has a LifeTime Component. After the Entity's life time expires, the Entity will be destroyed and its position queued for respawn at some later point.

Another part of the code shows loading Prefabs from code and creating Entities. Once a new Entity is created, it will be placed at the same position in the World where the originally spawned Entity was, but it will be colored green.

One additional important concept that this system illustrates is the use of an EntityCommandBuffer and a EntityCommandBufferSystem. To prevent race conditions, you cannot make _structural changes_ inside a Job. Structural changes include anything that changes the structure of your data, such as creating/destroying Entities or adding/removing Components. To overcome this limitation, ECS provides the EntityCommandBuffer.

Instead of performing structural changes directly, a Job can add a command to an EntityCommandBuffer to perform such changes on the main thread after the Job has finished. Command buffers allow you to perform any, potentially costly, calculations on a worker thread, while queuing up the actual insertions and deletions for later.

The BeginInitializationEntityCommandBufferSystem is a standard ECS System that provides an EntityCommandBuffer for any System to use. The ECB system automatically executes any commands in the command buffers it creates when the System runs (in this case, at the beginning of each frame).
