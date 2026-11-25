# HelloNetcode Go In Game sample

Going in game means enabling ghost snapshot synchronization, by adding the `NetworkStreamInGame` component to the connection entity. It has to be done on both the server connection to the client and on the client itself.

By default, Netcode does this automatically on both sides as soon as the connection is fully connected (has a `NetworkId`), controlled by the `NetcodeConfig.AutomaticConnectionInGame` setting on the `NetcodeConfig` asset. 

This sample demonstrates the manual flow, which is useful when a client needs to be ready before receiving snapshots (like loading the appropriate level running on the server already). To see the sample go in game manually, make sure **Automatic Connection In Game** on the `NetcodeConfig` asset is disabled.

A client needs to be ready to receive the snapshots from the server before enabling ghost replication. The `NetcodeConfig.GhostPrefabLoadingTimeout` is by default set to `-1` which will allow flexible ghost prefab loading on the server and connected clients, and `NetcodeConfig.AutomaticConnectionInGame` is safe to use with that behavior. It's unlikely to work well when strict ghost prefab loading is enabled (`GhostPrefabLoadingTimeout=0`). In that setup you should set up the game data on the server and clients so their loaded ghost prefab data matches exactly before going manually in-game.

See

* _Tie it together_ section in the [Getting Started](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/getting-started.html) guide
* [Network Connection](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/network-connection.html)
* _Prespawned ghosts_ section in the [Ghost snapshot](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/ghost-snapshots.html) docs.

## Requirements

Needs an established connection.

* Connection

## Sample description

Here a system just watches for new connections which aren't in game yet (have a `NetworkId` but no `NetworkStreamInGame`) and adds the `NetworkStreamInGame` component to them immediately when it sees them. 

With automatic in-game connections enabled (the default) no such connections exist, so the system doesn't do anything (but is harmless).
