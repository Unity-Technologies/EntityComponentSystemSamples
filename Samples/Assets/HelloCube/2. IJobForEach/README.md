# HelloCube: IJobForEach

This sample demonstrates a Job-based ECS System that rotates a pair of cubes.

## What does it show?

This sample builds on the ForEach sample and illustrates how to perform the same work in a multi-threaded Job rather than on the main thread.

As in the previous example, the **RotationSpeedSystem_IJobForEach** *updates* the object's rotation using the *data* stored in the **RotationSpeed_IJobForEach** Component.

## JobComponentSystems and IJobForEach

Systems using IJobForEach are the simplest, most efficient method you can use to process your Component data. We recommend starting with this approach for any System that you design.

In this example, RotationSpeedSystem_IJobForEach is implemented as a JobComponentSystem. The class creates an IJobForEach struct to define the work that needs to be done. This Job is scheduled in the System's OnUpdate() function.
