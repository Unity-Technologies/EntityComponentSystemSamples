# HelloCube: ForEach

This sample demonstrates a simple ECS System that rotates a pair of cubes.

## What does it show?

This sample demonstrates the separation of data and functionality in ECS. Data is stored in components, while functionality is written into systems.

The **RotationSpeedSystem_ForEach** *updates* the object's rotation using the *data* stored in the **RotationSpeed_ForEach** Component.

## ComponentSystems and Entities.ForEach

RotationSpeedSystem_ForEach is a ComponentSystem and uses an Entities.ForEach delegate to iterate through the Entities. This example only creates a single Entity, but if you added more Entities to the scene, the RotationSpeedSystem_ForEach updates them all — as long as they have a RotationSpeed_ForEach Component (and the Rotation Component added when converting the GameObject's Transform to ECS Components).

Note that ComponentSystems using Entities.ForEach run on the main thread. To take advantage of multiple cores, you can use a JobComponentSystem (as shown in the next HelloCube example).

## Converting from GameObject to Entity

The **ConvertToEntity** MonoBehaviour converts a GameObject and its children into Entities and ECS Components upon Awake. Currently the set of built-in Unity MonoBehaviours that ConvertToEntity can convert includes the Transform and MeshRenderer. You can use the **Entity Debugger** (menu: **Window** > **Analysis** > **Entity Debugger**) to inspect the ECS Entities and Components created by the conversion.

You can implement the IConvertGameObjectEntity interface on your own MonoBehaviours to supply a conversion function that ConvertToEntity will use to convert the data  stored in the MonoBehaviour to an ECS Component.

In this example, the **RotationSpeedAuthoring_ForEach** MonoBehaviour uses IConvertGameObjectEntity to add the RotationSpeed_ForEach Component to the Entity on conversion.
