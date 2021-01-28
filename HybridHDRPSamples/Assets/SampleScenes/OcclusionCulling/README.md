# OcclusionCulling

This sample demonstrates the new occlusion culling system on Entities.

<img src="../../../READMEimages/OcclusionCulling.PNG" width="600">

## What does it show?

In the scene, all the cubes are Occluders and Occludees. The cubes that are being culled by other cubes in front do not render. The workflow is different compared to the existing Umbra Occlusion Culling for GameObjects.

## How to use this sample scene?

1. Note that to use this sample you need to add the ENABLE_UNITY_OCCLUSION define symbol to the **Scripting Define Symbols** list in **Edit > ProjectSettings > Player > Other Settings**
2. In the Hierarchy, select the Subscene
3. In the Inspector, click Open
4. Select any cube, note that there are Occluder and Occludee components attached to the MeshRenderer
5. The MeshRenderers do not need to have the Static flag turned on, and do not need to bake for occlusion
6. Close the Subscene and enter Play mode
7. Move the camera and observe the **Culling Stats** in the Game view
8. Go to: **Top > Occlusion > Debug > Show Depth Test**
9. Red objects are the cubes that the new occlusion culling system culls

Note: This requires that you set the **Target 32** and **Target 64** bit architectures to **SSE4**. These two settings are in **Project Settings > Burst AOT Settings**.
