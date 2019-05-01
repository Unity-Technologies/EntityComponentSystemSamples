# HelloCube_08_SpawnAndRemove

This sample demonstrates spawning and removing entities from the world.

## What does it show?

1. Two different methods of adding and removing entities from the world. From
data, and from code by manually loading resource and creating entity.

2. Adding and removing entities from the world performance characteristic are
not changing due fragmentation of chunks.

3. Chunk utilization widget histogram activity in EntityDebugger.

When example starts entities are added into the world via entity spawner job in the same way as [HelloCube_06_SpawnFromEntity](../HelloCube_06_SpawnFromEntity#hellocube_06_spawnfromentity) demonstrates. In this example each red cube is entity that has life time component. After entity's life time expires entity will be destoryed and it's position queued for respawn at some later point. Other part of code shows loading prefab from code and creating entity. Once new entity is created it will be placed at the same position in the world where originally spawned entity was but it will be colored green.
