# Entities Graphics URP Samples Project
This Project includes feature sample Scenes for URP Entities Graphics.

## Feature sample Scenes
The feature sample Scenes are in the _SampleScenes_ folder split into different folders depending on topic. Most of these Scenes include GameObjects in a SubScene. 

Unity renders GameObjects with Entities Graphics when a corresponding entity exists, and without Entities Graphics when a corresponding entity does not exist. For the GameObjects in the SubScene, this means that Unity renders them with Entities Graphics at all times; in Edit Mode, in Play Mode, and in the built player.

### Scene List

| Scene | Description | Screenshot |
| --- | --- | - |
| AmbientAndBlendProbes | Demonstrate how to use light probes | ![](READMEimages/AmbientAndBlendProbes.PNG) |
| Lightmaps | Demonstrates lightmap support for Entities | ![](READMEimages/Lightmaps.PNG) |
| Lightprobes | Demonstrates lightprobe support for Entities | ![](READMEimages/Lightprobes.PNG) |
| BuiltInMaterialSHProperties | Demonstrates override of the built-in material SH property values | ![](READMEimages/BuiltInMaterialSHProperties.PNG) |
| MaterialOverridesSample | Demonstrates the setup of overriding a material's properties without having to write code | ![](READMEimages/MaterialOverridesSample.PNG) |
| ShaderGraphProperties | Demonstrates material property overrides for Shader Graph shaders on Entities | ![](READMEimages/ShaderGraphProperties.PNG) |
| URPLitProperties | Demonstrates material property overrides for different URP Lit material properties on Entities | ![](READMEimages/URPLitProperties.PNG) |
| HybridEntitiesConversion | Demonstrates the graphics related companion components that you can put in a Subscene | ![](READMEimages/HybridEntitiesConversion.PNG) |
| TriggerParticles | Demonstrates how to play a ParticleSystem from an ECS System | ![](READMEimages/TriggerParticles.PNG) |
| EntityCreation | Demonstrates how to efficiently create entities at run time that are rendered via Entities Graphics | ![](READMEimages/EntityCreation.PNG) |
| MaterialMeshChange | Demonstrates how to change a Material and Mesh on Entities at runtime | ![](READMEimages/MaterialMeshChange.PNG) |
| RenderMeshUtilityExample | Demonstrates the new RenderMeshUtility.AddComponents API | ![](READMEimages/RenderMeshUtilityExample.PNG) |
| MeshDeformations | Demonstrates BlendShape and SkinWeight entities | ![](READMEimages/MeshDeformations.PNG) |
| SkinnedCharacter | Demonstrates SkinnedMeshRenderer entities | ![](READMEimages/SkinnedCharacter.PNG) |
| DisabledEntities |  Demonstrates disabled entities | ![](READMEimages/DisabledEntities.PNG) |
| LODs | Demonstrates LODs in Entities Graphics | ![](READMEimages/LODs.PNG) |
| SimpleDotsInstancingShader | Demonstrates a simple unlit shader which renders using DOTS instancing | ![](READMEimages/SimpleDotsInstancingShader.PNG) |
| Submesh | Demonstrates using a Mesh with multiple sub-meshes with Entities Graphics | ![](READMEimages/Submesh.png) |
| TransparencyOrdering | Demonstrates transparent entities ordering | ![](READMEimages/TransparencyOrdering.PNG) |
