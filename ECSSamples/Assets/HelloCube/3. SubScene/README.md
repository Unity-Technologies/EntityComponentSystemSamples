# HelloCube: SubScene

This sample demonstrates the Sub Scene workflow.

Sub Scenes provide an efficient way to edit and load large game scenes in Unity.

## What does it show?

This sample uses the Components and Systems from HelloCube ForEach. The scene contains a pair of rotating cubes that are loaded automatically on play from a Sub Scene.

## Sub Scenes

When you save a scene, Unity converts any Sub Scenes to a native, binary format.
This format is memory-ready and can be loaded or streamed with only minimal alteration of the data in RAM. The format is ideally suited for streaming large amounts of Entities.

You can load a Sub Scene automatically on play. You can also defer loading until you stream the Sub Scene in from code (Using the RequestSceneLoaded component)

By default, Sub Scenes are loaded from the Entity binary files, even in the editor.

You can select a Sub Scene and click the **Edit** button in the Unity Inspector window to edit it.
While editing, you see the GameObject representations of the Entities in the Sub Scene in the Scene view and you can edit them as you would any GameObject.
A **live link** conversion pipeline applies any changes you make to the Game view scene.

The ability to edit only part of the scene and still have all the other Sub Scenes loaded as Entities for context, creates a very scalable workflow for editing massive scenes.

### To Create a Sub Scene

1. In the Unity Hierarchy window, right-click on empty space, or on a GameObject that you want to create the Sub Scene next to.

2. Select **New Sub Scene > Empty Scene...** in the context menu. Unity then creates an empty Sub Scene and creates a corresponding Scene Asset file in your project.

Click on the Sub Scene in the Hierarchy window to view its properties in the Inspector.
