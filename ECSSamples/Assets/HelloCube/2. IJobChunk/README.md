# HelloCube: IJobChunk

This sample demonstrates a Job-based ECS System that rotates a pair of cubes. Instead of iterating by Entity, this example iterates by chunk. (A Chunk is a block of memory containing Entities that all have the same Archetype — that is, they all have the same set of Components.)

## What does it show?

This sample shows another way to iterate through entities by accessing chunks directly.  This can offer more flexibility than Entities.ForEach in certain situations.

As in the previous examples, the **RotationSpeedSystem_IJobChunk** *updates* the object's rotation using the *data* stored in the **RotationSpeed_IJobChunk** Component.

This sample also demonstrates writing a custom authoring component.  This can provide more flexibility that generating one with the **GenerateAuthoringComponent** attribute.

## Systems and IJobChunk

In this example, the Job in RotationSpeedSystem_IJobChunk is now implemented using IJobChunk.

In a Job implemented with IJobChunk, the ECS framework passes an ArchetypeChunk instance to your Execute() function for each chunk of memory containing the required Components. You can then iterate through the arrays of Components stored in that chunk.

Notice that you have to do a little more manual setup for an IJobChunkJob. This includes constructing an EntityQuery that identifies which Component types the System operates upon. You must also pass ArchetypeChunkComponentType objects to the Job, which you then use inside the Job to get the NativeArray instances required to access the Component arrays themselves.

Systems using IJobChunk can handle more complex situations than those supported by Entities.ForEach, while maintaining maximum efficiency.

## Converting from GameObject to Entity

The **ConvertToEntity** MonoBehaviour converts a GameObject and its children into Entities and ECS Components upon Awake. Currently the set of built-in Unity MonoBehaviours that ConvertToEntity can convert includes the Transform and MeshRenderer. You can use the **Entity Debugger** (menu: **Window** > **Analysis** > **Entity Debugger**) to inspect the ECS Entities and Components created by the conversion.

You can implement the IConvertGameObjectEntity interface on your own MonoBehaviours to supply a conversion function that ConvertToEntity will use to convert the data  stored in the MonoBehaviour to an ECS Component.

In this example, the **RotationSpeedAuthoring_IJobChunk** MonoBehaviour uses IConvertGameObjectEntity to add the RotationSpeed_IJobChunk Component to the Entity on conversion.