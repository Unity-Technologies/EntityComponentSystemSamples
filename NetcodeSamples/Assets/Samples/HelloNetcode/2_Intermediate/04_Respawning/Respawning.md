# HelloNetcode Respawn Weapon sample

This samples shows how you can implement respawning players.

## Requirements

The spawn player sample is used to trigger the auto spawning of the player when connection is established. The dynamic physics objects from the physics sample are used.

* GoInGame
* SpawnPlayer
* Physics
* CharacterController
* ThinClients
* HitScanWeapon

## Sample description

This sample builds on the hit scan weapon sample and adds respawning. Run the sample with at least one thin client, which can be set in the multiplayer playmode tools.
You can aim and click the left mouse button to hit them. Five successful hits will be enough to knock them down. A successful hit can be seen by the hit marks as described in HitScanWeapon sample.
When dying the clients will do a rotation and then respawn at a random point on the map.

In the `RespawnSystem` the logic for respawning is showing how to destroy the old player entity and reconstructing this.
For netcode specific parts it is important to set these up again, i.e. if `CommandTargetComponent` and `LinkedEntityGroup` are not set up correctly, disconnecting the player after respawning will not despawn the entity correctly.

