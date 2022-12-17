# HelloCube sample

Very simple sample scenes demonstrating the basic elements of Entities.

<br>

### MainThread scene

The scene contains a single cube with a smaller child cube. The larger cube has a `RotationSpeedAuthoring` MonoBehavior, which adds a `RotationSpeed` IComponentData to the entity in baking.

At runtime, the `RotationSpeedSystem` spins all entities having the `RotationSpeed` component (in this case, the single parent cube).

<br>

### IJobEntity scene

This scene is the same as "MainThread", except the `RotationSpeedSystem` now uses a job (`IJobEntity`)named `RotationSpeedJob` to spin the cube instead of doing so directly on the main thread.

<br>

### Aspects scene

This scene is like the "MainThread" scene, except the `RotationSpeedSystem` now uses an aspect named `VerticalMovementAspect` to move the cube up and down.

<br>

### Prefabs scene

The scene contains a single non-rendered entity with a `Spawner` component that references a cube prefab.

At runtime, the `SpawnSystem` spawns many instances of the cube prefab and places the instances at random positions. The `RotationSpeedSystem` makes the cubes rotate and fall. The `FallAndDestroySystem` destroys cubes when they fall below y coord 0. When no cubes exist any more, `SpawnSystem` will spawn more cubes.

<br>

### IJobChunk scene

This scene is like "IJobEntity", but it uses `IJobChunk` instead of `IJobEntity`. Compared to `IJobEntity`, `IJobChunk` requires more boilerplate, but it provides more explicit control.

<br>

### Blob assets scene

This scene create a BlobAsset during Baking. At runtime, the animation curve BlobAsset is used to animate the y position of a cube.

The [MiscellaneousEntites project](../MiscellaneousEntities/README.md) includes a sample showing how to bake a BlobAsset in a baking system (which generally will scale better because baking systems can be Burst-compiled).

### Baking types scene

This scene doesn't do anything at runtime, but it uses baking to create a bounding box around each set of cubes (drawn as white debug lines). If you drag the cubes around, you'll see the bounding box updated as you drag.

With the `BakingTypeAttribute` you can convey data from Bakers to BakingSystems without having the data exist at runtime.

As `TemporaryBakingTypeAttribute` is removed after a single Baking pass, it can be used to create a reactive system.
Only when a specific Baker is re-run (the one that adds the `TemporaryBakingTypeAttribute`) does a BakingSystem re-run.
This can be done by having the BakingSystem require the `TemporaryBakingTypeAttribute` to run.

<br>

### GameObject sync scene

The scene contains an entity that references a GameObject instantiated from a prefab.

<br>
