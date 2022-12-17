# HelloNetcode Character Controller sample

This sample shows how you can use predicted spawning when clients instantiate networked objects. The server owns all networked objects or ghosts and normally would need to spawn them, but when the clients initiates an instantiation it can predict spawn the object and then swap it in when the ghost is received in a server snapshot.

This sample also demonstrates how you can implement a custom classification system instead of using the default one.

See


* [Prediction](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/prediction.html)
* _Implement Predicted Spawning for player spawned objects_ section in the [Ghost snapshot](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/ghost-snapshots.html) docs.

## Requirements

The spawn player sample is used to trigger the auto spawning of the player when connection is established. This is built on top of the character controller sample and uses the same inputs and character controller prefab.

* GoInGame
* SpawnPlayer
* CharacterController

## Sample description

This sample adds a grenade launcher to the player in the CharacterController sample. It adds a handler for the secondary fire inputs which spawns the grenades on both client and server in the prediction system.

Each grenade spawn is marked with a spawn ID (the input event counter with the network ID value of the connection). The custom classification system uses this spawn ID to match the locally predicted spawned grenades with the new spawns coming from ghost snapshots. When it matches it uses the already spawned object instead of spawning a new one like it would normally do.

The grenade itself is a physics object and will bounce and interact with other physics objects. When the launcher fires, a grenade is spawned and an initial physics velocity assigned to it. After that physics handles it. It's configured as a predicted ghost so it will collide with objects immediately on the client, but get corrected if the servers view differs.

After a certain interval the grenade is destroyed by the server and will push other physics object away depending on their distance, the clients do not predict this event. When the client detects the grenade is destroyed it will instantiate a particle explosion effect for it. This is only visual and so happens only on the client. The particle effect is a hybrid particle system, but needs to be manually destroyed after it's done playing one loop.

The spawned grenades will alternate between red and green coloring, and black for ones created via snapshot system and not swapped with the predict spawn. Just to help see that the swapping is not incorrect.

A configuration prefab is in the scene where various values can be tuned, like the initial velocity (power of the throw) for the grenade and it's fuse timer.