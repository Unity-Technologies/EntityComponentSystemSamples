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
