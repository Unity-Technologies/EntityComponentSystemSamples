# HelloNetcode Go In Game sample

Going in game means enabling ghost snapshot synchronization. A client needs to be ready to receive the snapshots from the server before this can be done (like loading the appropriate level running on the server already). It has to be done on both the server connection to the client and on himself.

See

* _Tie it together_ section in the [Getting Started](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/getting-started.html) guide
* [Network Connection](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/network-connection.html)
* _Prespawned ghosts_ section in the [Ghost snapshot](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/ghost-snapshots.html) docs.

## Requirements

Needs an established connection.

* Connection

## Sample description

Here a system just watches for new uninitialized connections and adds the `NetworkStreamInGame` component to them immediately when it sees them (and marks in initialized).
