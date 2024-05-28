Use the `StreamingStressTest` scene to test the creation and deletion of physics colliders at runtime.

When you open this scene for the first time you need to create the entities based on the GameObject cubes with physics peroperties. Select each of the active subscenes named _SubScene__* and in the inspector click "Rebuild Entities Cache". For large subscenes this can take up to 10 minutes or more. Whenever you modify a subscene you need to rebuild its entity cache again.

The sub-scenes _SubScene_StaticColliders_Fixed_Small_ and _SubScene_StaticColliders_Fixed_Large_ add to the main scene around 5000 and respectively 46000 static cubes. The larger scene is disabled by default.

_SubScene_StaticColliders_Streaming_ contains around 5000 static cubes and has a script attached to it that requests this subscene to be loaded or unloaded every `FramesBetweenStramingOperations = 10` frames. This simulates a scenario where parts of the game world get streamed in and out during gameplay.

/!\ Cubes will be flashing in the game view as they get streamed in and out frequently.
