# HelloNetcode prespawned ghosts sample

A prespawned ghost is simply a ghost which is already set up and networked when a netcode game starts. The data for each ghost instance is contained in the scene asset. This sample demonstrates how prespawned ghosts could be set up.

See

* _Prespawned ghosts_ section in the [Ghost snapshot](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/ghost-snapshots.html) docs.

## Requirements

The prespawned ghosts are configured when you go in-game so nothing happens until the `NetworkStreamInGame` component appears on the connection. This sample therefore requires:

* `GoInGame`

## Sample Description

A prespawned ghost must be an instance of a prefab and be placed in a `SubScene`. In the sample scene we have 3 variations of ghosts configured as such:

`PrespawnBarrel`

* Contains two ghost components one with a ghost field and one a buffer. Both can have pre-configured values or values adjusted in the scene instance (barrel prefab has the ghost field value set to 1 but scene changes it to 10000).

 `PrespawnBarrelWithNestedChild`

* Is like _PrespawnBarrel_ but with a prefab which is an instance of another prefab (_PrespawnBarrelChild_) and nested inside the main asset prefab. This contains a ghost field.
* The nested child prefab cannot have its own ghost authoring component, only the parent can.

`PrespawnBarrelWithSceneAdditions`

* Is an instance of _PrespawnBarrelWithNestedChild_ but with changes to the hierarchy made in the scene.
* Has another instance of the child prefab but added in the scene itself.
* Has a scene asset (not a prefab instance). This is valid as it's a child on a proper ghost prefab. This contains a ghost field as well.

There is also a movement system running on _PrespawnData_ which all the barrels have just to show that snapshot data is being applied and everything is connected properly.

All ghosts here are configured to use dynamic optimization mode (the default) which means they're expected to be updated frequently. See the ghost optimization sample to see when it makes sense to use the static opimization mode (ghosts rarely move) instead and what difference it makes.

## NOTE/TODO

* The nested child ghosts are always visible but the root/main barrel instance in the entity scene (subscene) is only visible in the scene view when the entity scene is open (as long as _Authoring State in Scene View_ is selected in the DOTS menu _Conversion Settings_)
