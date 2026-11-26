# GameObject Sample: 2D Physics

This sample shows how to network 2D physics with GameObjects. The server owns the simulation, and clients predict physics bodies, roll them back and re-simulate them when a snapshot corrects them. A predicted player moves and shoots boxes, so you can see how prediction and corrections behave under network conditions you set.

2D physics networking is experimental. The scene is `PhysicsCore2D/PhysicsCore2D.unity`, which uses `PhysicsBody` and `PhysicsShape` (Box2D v3, `Unity.U2D.Physics`).

## How it works

Netcode steps 2D physics once per prediction tick, inside the prediction loop, replayed ticks included. Each body's velocities, body type and sleep state are replicated and rolled back with its ghost. On the client, interpolated bodies are frozen to kinematic and follow snapshots.

PhysicsCore2D has no authoring components, so the sample creates each body in code, in `GhostPhysicsCore2DWorlds.GetOrCreateWorld(Ghost.World)` (one `PhysicsWorld` per netcode world), and attaches it to the prefab's `GhostPhysicsCore2DBody`.

Netcode creates every PhysicsCore2D world with contact warm starting off and contact recycle distance 0, because both carry solver state between steps that a rollback can't restore. The sample also sets 8 sub-steps, which resolve marginal contacts more consistently on client and server at some CPU cost.

## Requirements

Add the `NETCODE_GAMEOBJECT_PHYSICS2D_EXPERIMENTAL` scripting define.

## Running it

* **Host plus a Multiplayer Play Mode virtual player**: the host runs the server, and the virtual player is the client you watch. Host World Mode must be Single World: like other GameObject ghosts, the sample doesn't support a client and server as separate worlds in one editor.
* **Host only**: everything is authoritative, so there's nothing to predict.

Controls:

* A/D or the arrow keys to move, Space/W to jump.
* Press, drag back and release to fire a spinning box. It spawns where you release and flies towards where you pressed, faster the longer the arrow.
* F1 toggles the panel.

## Panel

* **Prediction mode (this client)**: switches the local player, remote players and boxes between predicted and interpolated, plus presets.
* **Corrections (this client)**: whether all predicted ghosts roll back together. The client also re-simulates rolled back ticks one at a time (no prediction step batching), which keeps the physics replay closer to what the server did.
* **Network simulator**: delay, jitter, loss and lag spikes, applied to this client only. Needs _Client Network Simulator_ enabled in _Multiplayer PlayMode Tools_ before entering play mode.
* **Boxes (server)**: sets the number of pyramid rows (n rows is n(n+1)/2 boxes), respawns the pyramid, and scatters the boxes.

## Files

* `Shared/`: the player, spawners, panel and start checks. `PlayerController2D` moves the player by setting velocity in `PredictionPhysicsUpdate`, which runs just before netcode's per-tick physics step. It talks to an `ISampleBody2D`, which `CoreBody2D` implements.
* `PhysicsCore2D/`: `CoreBody2D` creates a box body for each ghost in the netcode-managed world and attaches it to the prefab's `GhostPhysicsCore2DBody`. `CoreStaticBox2D` adds the floor and walls to every netcode-managed world.
