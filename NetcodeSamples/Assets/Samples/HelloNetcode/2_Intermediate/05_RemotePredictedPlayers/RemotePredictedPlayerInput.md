# HelloNetcode Remote Player Input Prediction

A players input can be predicted based on the previous values seen in his input buffer. This sample is exactly like the _Spawn Player_ sample before but tweaked to enable remote player input prediction feature.

See

* [Command stream](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/command-stream.html)
* _Authoring dynamic buffer serialization_ section in [Ghost snapshots](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/ghost-snapshots.html)

## Requirements

Connection needs to be in game already and scripts from the player spawning sample are re-used.

* GoInGame
* SpawnPlayer

## Sample description

The input struct and player prefab have changes for remote player prediction but otherwise this is the same sample as _Spawn Player_. On the prefab we have these changes:

* `Supported Ghost Modes` toggle. Changed to _Predicted_ since all the player ghosts are now only running in prediction mode and we don't spawn interpolated players.

Inputs are set up using `IInputComponentData` as in _Spawn Player_ but now each variable has the `[GhostField]` attribute set which will make the input buffer be synchronized to all non-owner players. The ghost component attributes doesn't need to be further tweaked as the default values are configured for this scenario. A `[GhostComponent]` attribute is now also added which specifies the input struct should only appear on the predicted ghost prefab (_AllPredicted_ prefab type).

The `RemotePredictedPlayerAutoCommands.cs` script is a duplicate of the _Spawn Player_ auto command script except it is configure to work on the `RemotePredictedPlayerInput` input struct.

## Notes

The players in this sample might look pretty much identical to the non-predicted players in the previous sample. To be sure inputs are actually synchronized you can look at the input buffers on the spawned remote players using the DOTS Hierarchy window.
