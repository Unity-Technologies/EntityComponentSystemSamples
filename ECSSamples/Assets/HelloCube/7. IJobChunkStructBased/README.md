# HelloCube: IJobChunkStructBased

This sample demonstrates a Job-based ECS System that rotates a pair of cubes.
Instead of iterating by Entity, this example iterates by chunk. (A Chunk is a
block of memory containing Entities that all have the same Archetype — that is,
they all have the same set of Components.)

It is the same example as we have seen previously in "2. IJobChunk", however
this time we are using `ISystemBase` which makes it possible to burst compile
the main thread update function as well.

## What does it show?

Primarily it illustrates the differences between class and struct based system
implementations.

