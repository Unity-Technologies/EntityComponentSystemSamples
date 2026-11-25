# HelloNetcode Hit-scan Weapon sample

This samples shows how you can implement a world space health bars.

## Requirements

The spawn player sample is used to trigger the auto spawning of the player when connection is established. The dynamic physics objects from the physics sample are used.
Character controller is used for moving the character around. 

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

`HealthBarSpawner` and `HealthUI` are unmanaged `struct IComponentData` that hold their `GameObject` / `Transform` / `Image` references via `UnityObjectRef<T>`, which is the supported way to point at a `UnityEngine.Object` from an unmanaged component. In `SpawnHealthBarSystem` we dereference the prefab with `.Value` and instantiate it with the regular `Object.Instantiate` method.

Because the spawned health-bar `GameObject` is owned by `HealthUI` and is not managed by netcode, we need to destroy it when the ghost entity goes away. We do that with a mirror `HealthUICleanup : ICleanupComponentData`: when the entity is destroyed, `HealthUI` is removed but `HealthUICleanup` survives, giving `HealthUICleanupSystem` a chance to destroy the GameObject and then remove the cleanup component. `HealthUICleanupSystem.OnDestroy` also sweeps any remaining cleanup components on world teardown, when the per-entity pass would otherwise never run again.

