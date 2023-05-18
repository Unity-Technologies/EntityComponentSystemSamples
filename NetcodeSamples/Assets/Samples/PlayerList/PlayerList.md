# PlayerList - Joiners and Leavers "Feed"

## Requirements
* "PlayerList" uses RPCs, which are available even "out of game".
* The "PlayerList" sample makes use of the optional `ICleanupComponentData` `ConnectionState`.

## Sample Description

The "PlayerList" sample uses RPCs to create a "feed" of players who have connected/disconnected, including the "reason" (i.e. cause) of the disconnect.

>[!NOTE]
> It is implemented via RPCs so that it can be used without the need to synchronise snapshots.
> However, another approach (which isn't demonstrated here) is to implement this behaviour via a single, static-optimized ghost (with an IBufferElementData containing all connected players).

### `ServerPlayerListSystem`
`ConnectionState` is added via an `OnUpdate` `AddComponent` call (using a query of all `NetworkStreamConnection` entities, which don't have a ConnectionState).

New Joiners are handled in the `HandleNewJoinersJob`. This job must wait for an RPC from a client (`ClientRegisterUsernameRpc`), as there is no way to infer a clients `Username` except to ask for it.
Thus, each client must opt-into being seen in the player list.
This username is then validated, and broadcast to all other players. The player is now considered "connected" (or "joined").

>[!NOTE]
> For your own game, you can source usernames however you like. 
> E.g. The GameServer could communicate with a backend instead, receiving a list of expected clients.
> Clients authenticate while connecting to the server, and are expected to provide a single-use join token, which can then be mapped to a Username.

For disconnecting players: The `NotifyPlayersOfDisconnectsJob` job polls each `ConnectionState` FSM, detects disconnects (regardless of the reason), and again broadcasts this change to clients.
Unity Transport Package (UTP) can detect:
* Client Disconnects.
* Server Disconnects (i.e. "Kicking" or "Booting" a player).
* Socket/Driver Timeouts (i.e. no response from a client for X seconds, assume disconnected).
This `DisconnectReason` is also broadcast to all other players, for you to optionally read from. We recommend communicating it to players, as it's a UX improvement: Players often appreciate knowing what happened to their friends.

### `ClientPlayerListSystem`
* Registers a username with the "PlayerList" sub-system (via `ClientRegisterUsernameRpc`).
* Receives and handles `InvalidUsernameResponseRpc` when attempting to set an invalid username (e.g. you may want to use a profanity or abuse filter).
* Receives and handles `PlayerListEntry.ChangedRpc` change events (of players connecting, disconnecting, and changing username).

### Rendering Systems
* `ClientPlayerListEventSystem` takes all `ChangedRpc`'s and converts them into `PlayerListNotificationBuffer` entries.
* `RenderPlayerListMb` is a stateless, debug, IMGUI renderer of this data. It also provides a text-field, allowing you to update your username.

### Testing
You can also use new controls in the "Multiplayer > Window: PlayMode Tools" to test various forms of disconnect (use thin clients to simulate other players).
Testing "Socket Timeouts" is now possible too. UTP defaults to 30s to timeout a connection.
Obviously, building a player to test this is also supported.

>[!NOTE]
> Effort has been taken to ensure that notifications are in order. I.e. When connecting, you will only see "new joiner" notifications for players who sent their connect notification RPCs _after you_.
