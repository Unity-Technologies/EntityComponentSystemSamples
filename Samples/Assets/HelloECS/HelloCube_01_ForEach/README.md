# HelloCube_01_ForEach

This sample demonstrates a simple ECS System that rotates a pair of cubes. 

## What does it show?

This sample demonstrates the separation of data and functionality in ECS. 
Data is stored in components, while functionality is written into systems.

The **RotationSpeedSystem** *updates* the object's rotation using the *data* stored in the **RotationSpeed** Component.

## Component Systems and ForEach

RotationSpeedSystem is a ComponentSystem and uses a ForEach delegate to iterate through the Entities. This example only creates a single Entity, but if you added more Entities to the scene, the RotationSpeedSystem updates them all — as long as they have a RotationSpeed Component (and the Rotation Component added when converting the GameObject's Transform to ECS Components).

Note that Component Systems using ForEach run on the main thread. To take advantage of multiple cores, you can use a JobComponentSystem (as shown in the next HelloECS examples).

## Converting from GameObject to Entity
 
 The **ConvertToEntity** MonoBehaviour converts a GameObject and its children into Entities and ECS Components on load. Currently the set of built-in Unity MonoBehaviours that ConvertToEntity can convert includes the Transform and  MeshRenderer. You can use the **Entity Debugger** (menu: **Window** > **Analysis** > **Entity Debugger**) to inspect the ECS Entities and Components created by the conversion.
 
 You can implement the IConvertGameObjectEntity interface on your own MonoBehaviours to supply a conversion function that ConvertToEntity will use to convert the data  stored in the MonoBehaviour to an ECS Component. 
 
 In this example, the **RotationSpeedProxy** MonoBehaviour uses IConvertGameObjectEntity to add the RotationSpeed Component to the Entity on conversion.


