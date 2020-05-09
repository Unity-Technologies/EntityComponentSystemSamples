# HelloCube: ForEach

This sample demonstrates a simple ECS System that rotates a pair of cubes.

## What does it show?

This sample demonstrates the separation of data and functionality in ECS. Data is stored in components, while functionality is written into systems.

The **RotationSpeedSystem_ForEach** *updates* the object's rotation using the *data* stored in the **RotationSpeed_ForEach** Component.

## Systems and Entities.ForEach

RotationSpeedSystem_ForEach is a system that derives from SystemBase and uses an Entities.ForEach lambda to iterate through the Entities. This example only creates a single Entity, but if you added more Entities to the scene, the RotationSpeedSystem_ForEach updates them all — as long as they have a RotationSpeed_ForEach Component (and the Rotation Component added when converting the GameObject's Transform to ECS Components).

Note that the system using Entities.ForEach uses ScheduleParallel to schedule the lambda to run on multiple worker threads if there are multiple chunks. This automatically takes advantage of multiple cores if they are available (and if your data spans multiple chunks).

## Converting from GameObject to Entity

This sample uses an automatically generated authoring component.  This MonoBehaviour is created via the **GenerateAuthoringComponent** attribute on the IComponentData and exposes all the public fields of that type for authoring.  See the **IJobChunk** sample for an example of how to write your own authoring component.