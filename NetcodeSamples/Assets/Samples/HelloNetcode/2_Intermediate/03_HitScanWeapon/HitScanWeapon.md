# HelloNetcode Hit-scan Weapon sample

This samples shows how you can implement a hit-scan weapon (instant hit, zero size projectile) with lag compensation.

See

* [Physics](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/physics.html)
* [Unity Physics](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/TableOfContents.html) package

## Requirements

The spawn player sample is used to trigger the auto spawning of the player when connection is established. The dynamic physics objects from the physics sample are used.

* GoInGame
* SpawnPlayer
* Physics
* CharacterController

## Sample description

This sample builds on the character controller sample and adds a hit-scan weapon. The hit-scan weapon will perform a raycast to determine where the weapon hit. In order to handle the time defference between interpolated and predicted characters without forcing players to lead their targets the server uses lag compensation - which means it performs the raycast against something close to what the player saw on their screen when firing.

The sample has a checkbox to toggle lag compensation to demonstrate the effect, make sure you are testing with simulated latency to see the effect.
When the hit-scan weapon hits it will spawn  hit marker showing the hit position using a + for client hit and a x for server hit.
