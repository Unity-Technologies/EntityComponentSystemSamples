# HelloNetcode Hit-scan Weapon sample

This samples shows how you can implement a world space health bars.

## Requirements

The spawn player sample is used to trigger the auto spawning of the player when connection is established. The dynamic physics objects from the physics sample are used.
Character controller is used for moving the character around. 

* GoInGame
* SpawnPlayer
* Physics
* CharacterController
* HitScanWeapon
* Respawning

## Sample description

This sample builds on the hit scan weapon sample and the respawn sample which added health. This sample shows how to display this health component as a world space health bar.
To see this enter play mode with at least one thin client enabled in the play mode tools window. You will see that above all players a health bar is visible, a black background and a green bar within.

As you shoot and hit you will see the health bar tick down until it is empty which will trigger the respawning as described in the sample named Respawning.
The health will respawn at full health together with the character in its new position.

## Note

The `HealthBarSpawnerAuthoring` is creating a IComponent as a class rather than a struct. This is because we need to maintain the reference to the gameobject of the healthbar prefab.
In the `SpawnHealthBarSystem` we instantiate the gameobject using the regular Object.Instantiate method. 
