# Entities Graphics HDRP Samples Project
This Project includes feature sample Scenes, stress test Scenes, and unit tests for HDRP Entities Graphics.

## Feature sample Scenes
The feature sample Scenes are in the _SampleScenes_ folder. To ensure full workflow coverage, most of these Scenes include GameObjects in a SubScene and GameObjects that have a ConvertToEntity component. 

Unity renders GameObjects with Entities Graphics when a corresponding DOTS entity exists, and without Entities Graphics when a corresponding DOTS entity does not exist. For the GameObjects in the SubScene, this means that Unity renders them with Entities Graphics at all times; in Edit Mode, in Play Mode, and in the built player.

### Scene List

| Scene | Description | Screenshot |
| --- | --- | - |
| AddComponentsExample | Demonstrates the new RenderMeshUtility.AddComponents API | ![](READMEimages/AddComponentsExample.PNG) |
| BuiltinProperties | Demonstrates override of the built-in material SH property values | ![](READMEimages/BuiltinProperties.PNG) |
| MeshDeformations | Demonstrates BlendShape and SkinWeight entities | ![](READMEimages/Deformations.PNG) |
| DisabledEntities |  Demonstrates disabled entities | ![](READMEimages/DisabledEntities.PNG) |
| EntityCreationAPI | Demonstrates how to efficiently create entities at run time that are rendered via Entities Graphics  | ![](READMEimages/EntityCreationAPI.PNG) |
| HybridEntitiesConversion | Demonstrates the graphics related Hybrid entities that you can put in a Subscene | ![](READMEimages/HybridEntitiesConversion.PNG) |
| Lightmaps | Demonstrates lightmap support for Entities | ![](READMEimages/Lightmaps.PNG) |
| Lightprobes | Demonstrates lightprobe support for Entities | ![](READMEimages/Lightprobes.PNG) |
| LODs | Demonstrates LODs in Entities Graphics| ![](READMEimages/LODs.PNG) |
| MaterialMeshChange | Demonstrates how to change a Material and Mesh on Entities at runtime | ![](READMEimages/MaterialMeshChange.PNG) |
| MaterialOverridesSample | Demonstrates the setup of overriding a material's properties without having to write code | ![](READMEimages/MaterialOverridesSample.PNG) |
| MatrixPrevious | Demonstrates moving Entities and support for HDRP Motion Vectors | ![](READMEimages/MatrixPrevious.PNG) |
| ShaderGraphProperties | Demonstrates material property overrides for Shader Graph shaders on Entities | ![](READMEimages/ShaderGraphProperties.PNG) |
| SkinnedMeshes | Demonstrates SkinnedMeshRenderer entities | ![](READMEimages/SkinnedMeshes.PNG) |
| TransparencyOrdering | Demonstrates transparent entities ordering | ![](READMEimages/TransparencyOrdering.PNG) |
| TriggerParticles | Demonstrates how to play VFX from an ECS System | ![](READMEimages/TriggerParticles.PNG) |
| HDRPLitProperties | Demonstrates material property overrides for different HDRP Lit material properties on Entities | ![](READMEimages/HDRPLitProperties.PNG) |
| HDRPShaders | Demonstrates material property overrides for HDRP Lit, LitTessellation, Unlit, LayeredLit, LayeredLitTessellation shaders on Entities | ![](READMEimages/HDRPShaders.PNG) |
