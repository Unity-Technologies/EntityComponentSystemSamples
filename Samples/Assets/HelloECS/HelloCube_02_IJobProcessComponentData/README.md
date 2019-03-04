# HelloCube_02_IJobProcessComponentData

This sample demonstrates a Job-based ECS System that rotates a pair of cubes. 

## What does it show?

This sample builds on HelloCube_01_ForEach and illustrates how to perform the same work in a multi-threaded Job rather than on the main thread.

As in the previous example, the **RotationSpeedSystem** *updates* the object's rotation using the *data* stored in the **RotationSpeed** Component.

## Job Component Systems and IJobProcessComponentData

Systems using IJobProcessComponentData are the simplest efficient method you can use to process your  Component data. We recommend starting with this approach for any System that you design.

In this example, RotationSpeedSystem is now implemented as a JobComponentSystem. The class creates an IJobProcessComponentData struct to define the work that needs to be done. This Job is scheduled in the System's OnUpdate() function.  

