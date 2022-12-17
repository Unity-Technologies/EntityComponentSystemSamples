# HelloNetcode Character Controller sample

This samples shows how you can expand kinematic character controllers as used in the Physics sample to allow characters to react to and walk on static geometry. The motion is still using kinematic physics, but it checks if it is standing on something and makes sure to not go through obstacles.

See

* [Physics](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/physics.html)
* [Unity Physics](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/TableOfContents.html) package

## Requirements

The spawn player sample is used to trigger the auto spawning of the player when connection is established. The dynamic physics objects from the physics sample are used.

* GoInGame
* SpawnPlayer
* Physics

## Sample description

This sample works very similar to the Physics sample, the only difference is how the character is moved. The character has a reference to a character controller config which has a separate physics collider. The collider used for the character controller only collides with static geometry, this means that the character controller can perform collider casts to find a valid position to move to. The actual movement is performed by setting a physics velocity on the real collider and let physics move it. This means all interactions with dynamic objects are performed, but they do not affect the movement of the player.

The character controller prefab has been modified compared to the physics player sample like so:

* Add a CharacterControllerAuthoring with a reference to a new prefab which has a CharacterControllerConfigAuthoring

The CharacterControllerConfig is a separate prefab which has a collider setup to only collide with static objects, and some configuration parameters for movement.
