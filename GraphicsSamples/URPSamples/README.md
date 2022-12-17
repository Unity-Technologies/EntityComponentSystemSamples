# Entities Graphics URP Samples Project
This Project includes feature sample Scenes for Entities Graphics.

## Feature sample Scenes
The feature sample Scenes are in the _SampleScenes_ folder. Most of these Scenes include GameObjects in a SubScene. 

Unity renders GameObjects with Entities Graphics when a corresponding DOTS entity exists, and without Entities Graphics when a corresponding DOTS entity does not exist. For the GameObjects in the SubScene, this means that Unity renders them with Entities Graphics at all times; in Edit Mode, in Play Mode, and in the built player.

### Scene List

| Scene | Description | Screenshot |
| --- | --- | - |
| AddComponentsExample | Demonstrates the new RenderMeshUtility.AddComponents API | ![](READMEimages/AddComponentsExample.PNG) |
| BuiltinProperties | Demonstrates override of the built-in material SH property values | ![](READMEimages/BuiltinProperties.PNG) |
| DisabledEntities |  Demonstrates disabled entities | ![](READMEimages/DisabledEntities.PNG) |
| EntityCreationAPI | Demonstrates how to efficiently create entities at run time that are rendered via Entities Graphics | ![](READMEimages/EntityCreationAPI.PNG) |
| HybridEntitiesConversion | Demonstrates the graphics related Hybrid entities that you can put in a Subscene | ![](READMEimages/HybridEntitiesConversion.PNG) |
| Lightmaps | Demonstrates lightmap support for Entities | ![](READMEimages/Lightmaps.PNG) |
| Lightprobes | Demonstrates lightprobe support for Entities | ![](READMEimages/Lightprobes.PNG) |
| LODs | Demonstrates LODs in Entities Graphics | ![](READMEimages/LODs.PNG) |
| MaterialMeshChange | Demonstrates how to change a Material and Mesh on Entities at runtime | ![](READMEimages/MaterialMeshChange.PNG) |
| MaterialOverridesSample | Demonstrates the setup of overriding a material's properties without having to write code | ![](READMEimages/MaterialOverridesSample.PNG) |
| MeshDeformations | Demonstrates BlendShape and SkinWeight entities | ![](READMEimages/Deformations.PNG) |
| ShaderGraphProperties | Demonstrates material property overrides for Shader Graph shaders on Entities | ![](READMEimages/ShaderGraphProperties.PNG) |
| SimpleDotsInstancingShader | Demonstrates a simple unlit shader which renders using DOTS instancing | ![](READMEimages/SimpleDotsInstancingShader.PNG) |
| SkinnedCharacter | Demonstrates SkinnedMeshRenderer entities | ![](READMEimages/SkinnedMeshes.PNG) |
| TransparencyOrdering | Demonstrates transparent entities ordering | ![](READMEimages/TransparencyOrdering.PNG) |
| TriggerParticles | Demonstrates how to play a ParticleSystem from an ECS System | ![](READMEimages/TriggerParticles.PNG) |
| URPLitProperties | Demonstrates material property overrides for different URP Lit material properties on Entities | ![](READMEimages/URPLitProperties.PNG) |
