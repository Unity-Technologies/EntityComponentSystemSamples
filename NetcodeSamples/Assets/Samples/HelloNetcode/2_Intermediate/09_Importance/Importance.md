# HelloNetcode Importance sample

This sample showcase how to set up the built-in **Distance Importance Scaling** feature.

See

* [Optimization Mode's GhostAuthoringInspector](https://docs.unity3d.com/Packages/com.unity.netcode@1.0/manual/ghost-snapshots.html#authoring-ghosts)
* [Further reading about this optimization](https://docs.unity3d.com/Packages/com.unity.netcode@1.0/manual/optimizations.html#importance-scaling)

## Requirements

* GoInGame
* Optimization, for barrel spiral spawning logic

## Sample description

This sample spawns a number of barrels rotating in the scene when entering play mode. By using this densely populated scene we demonstrate how the behaviour of importance works.
The number of barrels spawned can be changed by opening the subscene and changing the parameters on the `BarrelSetup`'s inspector.

- **Amount Of Circles**: can be used to add an extra circle of barrels when entering play mode.
- **Spacing**: changes the distance between the barrels for better visibility.

The barrels are spawned by the server, and do not contain any physics components. 
Each barrel is being rotated on the server, and therefore, each barrel's transform will be synchronized to the client (which will then render it).

When looking at the barrels in the scene view, it is possible to see that some of the barrels in the outer edge of the blob are not rotating as smoothly as the ones towards the center. 
Depending on the view it might be necessary to zoom out a bit.

The barrels are automatically grouped by a 3D, `int3` tiling in their Entities chunks. I.e. They are moved into spatially located chunks.
Then, each of these tiles (i.e. chunks) will be updated according to the distance (of the entire chunk) to each connection, based on an algorithm in `GhostDistanceImportance.Scale`.

The setup for importance scaling is in `UpdateConnectionPositionSystem.OnUpdate`. 
Here, an entity is being created, with two components; namely `GhostDistanceData` and `GhostImportance`. 
Both of these components are expected by the `GhostDistanceImportance.Scale` callback.
> ![NOTE]
> The `EnableImportance` flag component singleton must exist in the scene for this call to be triggered. Importantly, we must put this check in `OnUpdate`, as loading a sub-scene is an async operation most of the time. The exception to this is when in editor, when manually loading a sub-scene.
 
The last importance component is `GhostConnectionPosition`, which must be added to all connection entities on the server (see `UpdateConnectionPositionSystem.cs` in this sample).
This component's value denotes the position that should be considered the most important point for that client, 
thus allowing the `GhostSendSystem` (and the `GhostChunkSerializer`) the ability to determine the importance center, 
when building each snapshot for each client.

I.e. From this point, the distance based scaling will calculate the distance to each tiles center, 
thus determining the importance multiplayer to apply for all entities within this tile (i.e. chunk).

## Note

> [!NOTE]
> Your PC specs will determine how many barrels your PC can comfortably spawn, so you may need to tweak spawn values to make this importance scaling more clear.
> We also recommend testing this with Burst enabled, as the effect that we're trying to demonstrate here is related to latency, and therefore CPU throttling can add unwanted noise.

Depending on the PC simulating this sample, the center being updated smoothly might decrease/increase. By spawning less and fewer barrels from the BarrelSetup component in the subscene it can increase/decrease the number of being spawned.

The tile size configuration can be changed as well to show the impact of these changes.

It is possible to create a custom importance scale implementation, and switch out all components to fit other use cases. The only shipped implementation is the distanced based importance with square tiles as this covers most simple use cases.

The `GhostConnectionPosition` would in a typical game follow the character around, and it will be necessary to update the `GhostConnectionPosition` component with this information. This will add the behaviour of the barrels closest to the player will be updated more often.

## **BarrelWithoutImportance**
You probably noticed that the red, sparsely spawned 'BarrelWithoutImportance' barrels are **not** being forced to a lower send rate. 
They are explicitly filtered out of the importance scaling sub-system.

To opt-out specific ghosts from importance scaling, you must do the following:
1. Set `GhostDistancePartitioningSystem.AutomaticallyAddGhostDistancePartitionSharedComponent` to `false` (or use your own, bespoke system). 
This will disable the `GhostDistancePartitioningSystem`s default behaviour of adding the `GhostDistancePartitionShared` shared component to all ghost instances that meet its criteria.
2. Do not add the `GhostDistancePartitionShared` to these ghost instances (i.e. note the **absence** of the `EnableImportanceScalingOnThisGhost` authoring on the **BarrelWithoutImportance** prefab).

This sample showcase how to set up the built-in **Distance Importance Scaling** feature.
* [Optimization Mode's GhostAuthoringInspector](https://docs.unity3d.com/Packages/com.unity.netcode@1.0/manual/ghost-snapshots.html#authoring-ghosts)
* [Further reading about this optimization](https://docs.unity3d.com/Packages/com.unity.netcode@1.0/manual/optimizations.html#importance-scaling)
This sample spawns a number of barrels rotating in the scene when entering play mode. By using this densely populated scene we demonstrate how the behaviour of importance works.
- **Amount Of Circles**: can be used to add an extra circle of barrels when entering play mode.
The barrels are spawned by the server, and do not contain any physics components. 
Each barrel is being rotated on the server, and therefore, each barrel's transform will be synchronized to the client (which will then render it).
When looking at the barrels in the scene view, it is possible to see that some of the barrels in the outer edge of the blob are not rotating as smoothly as the ones towards the center. 
Depending on the view it might be necessary to zoom out a bit.
The barrels are automatically grouped by a 3D, `int3` tiling in their Entities chunks. I.e. They are moved into spatially located chunks.
Then, each of these tiles (i.e. chunks) will be updated according to the distance (of the entire chunk) to each connection, based on an algorithm in `GhostDistanceImportance.Scale`.
The setup for importance scaling is in `UpdateConnectionPositionSystem.OnUpdate`. 
Here, an entity is being created, with two components; namely `GhostDistanceData` and `GhostImportance`. 
Both of these components are expected by the `GhostDistanceImportance.Scale` callback.
This component's value denotes the position that should be considered the most important point for that client, 
thus allowing the `GhostSendSystem` (and the `GhostChunkSerializer`) the ability to determine the importance center, 
when building each snapshot for each client.
I.e. From this point, the distance based scaling will calculate the distance to each tiles center, 
thus determining the importance multiplayer to apply for all entities within this tile (i.e. chunk).

## **BarrelWithoutImportance**
You probably noticed that the red, sparsely spawned 'BarrelWithoutImportance' barrels are **not** being forced to a lower send rate. 
They are explicitly filtered out of the importance scaling sub-system.

To opt-out specific ghosts from importance scaling, you must do the following:
1. Set `GhostDistancePartitioningSystem.AutomaticallyAddGhostDistancePartitionSharedComponent` to `false` (or use your own, bespoke system). 
This will disable the `GhostDistancePartitioningSystem`s default behaviour of adding the `GhostDistancePartitionShared` shared component to all ghost instances that meet its criteria.
2. Do not add the `GhostDistancePartitionShared` to these ghost instances (i.e. note the **absence** of the `EnableImportanceScalingOnThisGhost` authoring on the **BarrelWithoutImportance** prefab).

## Importance Visualizer
The `Importance Visualizer` is a PlaymodeTool's drawer that helps visualize Importance Scaling outcomes.

Supported modes are:
* **PerEntityHeatmap** - Draws a per-entity heatmap, denoting the importance scaling applied to this entire chunk.
  Supports custom importance scaling structs.
* **PerEntitySpatialChunkStructure** - Assigns a random color for each chunk, and draws said random color for all entities in that chunk, as well as lines linking them to each other.
  Supports custom importance scaling structs.
* **DrawGrid** - Draws a flat  heatmap of the GhostDistanceData` tiles used by the `GhostDistancePartitioningSystem`. 
Thus, only functional when using the default importance scaling function.

