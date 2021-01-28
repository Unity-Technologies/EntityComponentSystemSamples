# Hybrid HDRP Samples Project
This Project includes feature sample Scenes, stress test Scenes, and unit tests for the HDRP Hybrid Renderer.

## Feature sample Scenes
The feature sample Scenes are in the _SampleScenes_ folder. To ensure full workflow coverage, most of these Scenes include GameObjects in a SubScene and GameObjects that have a ConvertToEntity component. 

Unity renders GameObjects with the Hybrid Renderer when a coresponding DOTS entity exists, and without the Hybrid Renderer when a corresponding DOTS entity does not exist. For the GameObjects in the SubScene, this means that Unity renders them with the Hybrid Renderer at all times; in Edit Mode, in Play Mode, and in the built player. For the GameObjects with a ConvertToEntity component, this means that Unity renders them with the Hybrid Renderer only in Play Mode and in the built player. This is because Unity converts the components to DOTS entities at runtime.

## Stress test scenes
Stress test Scenes are located in the _StressTestScenes_ folder.

The _BigBatches_ stress test measures maximum thoughput of perfectly batched content. _StressTestGameObjects_ uses GameObjects, and _StressTestHybrid_ uses the Hybrid Renderer. This allows you to measure performance differences between GameObject rendering and Hybrid Rendering. 

The stress test scene contains 100,000 spawned boxes. They cycle though four different animation modes: no animation, color animation, position animation, color + position animation. 

_StressTestGameObjects_ has two variants: one that includes the color overrides, and one that does not incude color overrides. This is because modifying GameObject color requires Material changes per GameObject, which causes material replication per object and has a large performance impact. Providing a variant without color overrides demonstrates more realistic performance for cases where you only modify position.

## Compatibility
Compatible with Unity 2020.2.3f1 and HDRP 10.2.2 or later.\
Requires Hybrid Renderer V2.\
For instructions of Enabling Hybrid Renderer V2, see the [documentation](https://docs.unity3d.com/Packages/com.unity.rendering.hybrid@latest/index.html).