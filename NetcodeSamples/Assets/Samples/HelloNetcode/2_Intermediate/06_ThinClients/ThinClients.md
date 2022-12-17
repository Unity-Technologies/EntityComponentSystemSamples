# HelloNetcode Thin Client Sample

A thin client is a stripped down client which only sends input to the server and does not run anything else. It receives ghost snapshot updates from the server but discards them (no processing done). Thus it does not spawn anything.

Thin clients are only supported in the editor and make it easier to develop multiplayer features in the editor with simulated extra clients so you don't need to build and launch them as a standalone. You can set the amount of thin clients you want in the _Playmode Tools_ found in the _Multiplayer_ menu.

See

* _Thin Clients_ in the [Client Server Worlds](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/client-server-worlds.html) section
* _Automatic command input setup using IInputComponentData_ section in the [Command Stream](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/command-stream.html) section

## Requirements

Connection needs to be in game already and re-uses the spawn player sample so you don't need to spawn anything (just set up new inputs)

* GoInGame
* SpawnPlayer

## Sample description

This sample shows how to add thin client random input generation to a project when using the [__IInputComponentData__](xref:Unity.NetCode.IInputComponentData) component for input handling.

To implement the input handling of a thin client you need to manually create a dummy player entity to contain the input logic. The dummy entity needs to have the bare minimum functionality for it to work properly, that's the input component/buffer and have the command target and ghost owner components configured for the thin clients own player (`CreateThinClientPlayer` function in the sample).

In this sample the random inputs just move the player around and make it jump at regular intervals. It has no idea about other players or its surroundings. How the random input is generated is then of course dependent on the game being developed. You just have to keep in mind you have no up to date game data available as ghost snapshots are not being processed and thus can't depend on that state.
