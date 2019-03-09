# HelloCube_03_IJobChunk

This sample demonstrates a Job-based ECS System that rotates a pair of cubes. Instead of iterating by Entity, this example iterates by chunk. (A Chunk is a block of memory containing Entities that all have the same Archetype — that is, they all have the same set of Components). 

## What does it show?

This sample builds on HelloCube_02_IJobProcessComponentData. It uses a Job based on IJobChunk.

As in the previous examples, the **RotationSpeedSystem** *updates* the object's rotation using the *data* stored in the **RotationSpeed** Component.

## Job Component Systems and IJobChunk

In this example, the Job in RotationSpeedSystem is now implemented using IJobChunk.   

In a Job implemented with IJobChunk, the ECS framework passes an ArchetypeChunk instance to your Execute() function for each chunk of memory containing the required Components. You can then iterate through the arrays of Components stored in that chunk.

Notice that you have to do a little more manual setup for an IJobChunkJob. This includes constructing a ComponentGroup that identifies which Component types the System operates upon. You must also pass ArchetypeChunkComponentType objects to the Job, which you then use inside the Job to get the NativeArray instances required to access the Component arrays themselves. 

Systems using IJobChunk can handle more complex situations than those supported by IJobProcessComponentData, while maintaining maximum efficiency. 


