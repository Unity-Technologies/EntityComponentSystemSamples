# HelloNetcode Spawn Player with Auto Commands sample

A player can be spawned for each client on the server. Certain functionality works specifically with this type of entity with regard to lag compensation techniques like client side prediction (but can be applied to other entities as well). Is sometimes referred to as the entity the client owns.

Input commands can be more easily set up by using the Auto Commands feature toggled on the ghost authoring prefab and making use of `IInputComponentData` for storing input data. Only one entity can send inputs to the server, usually the player character.

See

* [Entity spawning](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/ghost-snapshots.html)
* [Command stream](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/command-stream.html)

## Requirements

Connection needs to be in game already.

* GoInGame

## Sample description

The ghost authoring component has the following tweaks from the default values:

* `Has Owner` toggle. This makes only the client run prediction on his own player instance, and not remote player instances of the prefab.
* `Support Auto Commands` toggle. This makes the command target automatically set on the connection (pointing to the player entity owned by the client). Setting up the command target is required to properly send commands.

As soon as a connection is detected on the server which is _in game_ but does not yet have the `PlayerSpawned` component, a player is spawned for it. The ghost snapshot replication then makes sure it's also spawned on all clients.

Inputs are set up using `IInputComponentData`. This ensures they are automatically set up as commands (via code generation) which will be collected on the client and placed in the command buffer. The input can then be processed also on the server (or other players with remote player prediction), without any extra work.

Any `[GhostComponent]` attribute values set on the `IInputComponentData` input struct will be applied to the input buffer as well but this sample is using the defaults.
