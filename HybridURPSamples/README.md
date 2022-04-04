# Hybrid URP Samples Project
This Project includes feature sample Scenes, stress test Scenes, and unit tests for the URP Hybrid Renderer.\
Please visit official [Hybrid Renderer documentation](https://docs.unity3d.com/Packages/com.unity.rendering.hybrid@0.50/manual/index.html) for more details.

## Feature sample Scenes
The feature sample Scenes are in the _SampleScenes_ folder. To ensure full workflow coverage, most of these Scenes include GameObjects in a SubScene and GameObjects that have a ConvertToEntity component. 

Unity renders GameObjects with the Hybrid Renderer when a coresponding DOTS entity exists, and without the Hybrid Renderer when a corresponding DOTS entity does not exist. For the GameObjects in the SubScene, this means that Unity renders them with the Hybrid Renderer at all times; in Edit Mode, in Play Mode, and in the built player. For the GameObjects with a ConvertToEntity component, this means that Unity renders them with the Hybrid Renderer only in Play Mode and in the built player. This is because Unity converts the components to DOTS entities at runtime.

### Scene List

| Scene | Description | Screenshot |
| --- | --- | - |
| AddComponentsExample | Demonstrates the new RenderMeshUtility.AddComponents API | ![](READMEimages/AddComponentsExample.PNG) |
| BuiltinProperties | Demonstrates override of the built-in material SH property values | ![](READMEimages/BuiltinProperties.PNG) |
| DisabledEntities |  Demonstrates disabled entities | ![](READMEimages/DisabledEntities.PNG) |
| EntityCreationAPI | Demonstrates how to efficiently create entities at run time that are rendered via Hybrid Renderer  | ![](READMEimages/EntityCreationAPI.PNG) |
| HybridEntitiesConversion | Demonstrates the graphics related Hybrid entities that you can put in a Subscene | ![](READMEimages/HybridEntitiesConversion.PNG) |
| Lightmaps | Demonstrates lightmap support for Entities | ![](READMEimages/Lightmaps.PNG) |
| Lightprobes | Demonstrates lightprobe support for Entities | ![](READMEimages/Lightprobes.PNG) |
| LODs | Demonstrates LODs and HLODs in Hybrid Renderer | ![](READMEimages/LODs.PNG) |
| LODsStatic | Demonstrates static optimized LOD hierarchies | ![](READMEimages/LODs.PNG) |
| MaterialMeshChange | Demonstrates how to change a Material and Mesh on Entities at runtime | ![](READMEimages/MaterialMeshChange.PNG) |
| MaterialOverridesSample | Demonstrates the setup of overriding a material's properties without having to write code | ![](READMEimages/MaterialOverridesSample.PNG) |
| MeshDeformations | Demonstrates BlendShape and SkinWeight entities | ![](READMEimages/Deformations.PNG) |
| OcclusionCulling | Demonstrates the new occlusion culling system on Entities | ![](READMEimages/OcclusionCulling.PNG) |
| ShaderGraphProperties | Demonstrates material property overrides for Shader Graph shaders on Entities | ![](READMEimages/ShaderGraphProperties.PNG) |
| SharedComponentOverrides | Demonstrates the support for overriding DOTS instanced material properties using ISharedComponentData | ![](READMEimages/SharedComponentOverrides.PNG) |
| SimpleDotsInstancingShader | Demonstrates a simple unlit shader which renders using DOTS instancing | ![](READMEimages/SimpleDotsInstancingShader.PNG) |
| SkinnedCharacter | Demonstrates SkinnedMeshRenderer entities | ![](READMEimages/SkinnedMeshes.PNG) |
| TransparencyOrdering | Demonstrates transparent entities ordering | ![](READMEimages/TransparencyOrdering.PNG) |
| TriggerParticles | Demonstrates how to play a ParticleSystem from an ECS System | ![](READMEimages/TriggerParticles.PNG) |
| URPLitProperties | Demonstrates material property overrides for different URP Lit material properties on Entities | ![](READMEimages/URPLitProperties.PNG) |
| URPShaders | Demonstrates material property overrides for URP Lit/Unlit shaders on Entities | ![](READMEimages/URPShaders.PNG) |

## Stress test scenes
Stress test Scenes are located in the _StressTestScenes_ folder.

The _BigBatches_ stress test measures maximum thoughput of perfectly batched content. _StressTestGameObjects_ uses GameObjects, and _StressTestHybrid_ uses the Hybrid Renderer. This allows you to measure performance differences between GameObject rendering and Hybrid Rendering. 

The stress test scene contains 100,000 spawned boxes. They cycle though four different animation modes: no animation, color animation, position animation, color + position animation. 

_StressTestGameObjects_ has two variants: one that includes the color overrides, and one that does not incude color overrides. This is because modifying GameObject color requires Material changes per GameObject, which causes material replication per object and has a large performance impact. Providing a variant without color overrides demonstrates more realistic performance for cases where you only modify position.

## Compatibility
Compatible with Unity 2020.3.30f1 and URP 10.8.1 or later.
