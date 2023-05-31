# HelloCube sample

[Video: Entities "HelloCube" samples walkthrough](https://youtu.be/32TLgtA9yUM) (30 minutes)

*These very simple samples demonstrate the basic elements of the Entities API.*

## MainThread sample

The sample rotates two cubes, a parent and its child. 

The larger cube has a `RotationSpeedAuthoring` MonoBehavior, which adds a `RotationSpeed` IComponentData to the entity in baking. At runtime, the `RotationSpeedSystem` spins all entities having the `RotationSpeed` component (in this case, just the single parent cube).

## IJobEntity sample

This sample is the same as "MainThread", except the `RotationSpeedSystem` now uses a job (an `IJobEntity`) to spin the cubes instead of doing so directly on the main thread. Also, the Y-axis scale of the cube is fluctuated between 1 and -1 by setting the cube's `PostTransformMatrix` component. 

## Aspects sample

This sample is like "MainThread", except the `RotationSpeedSystem` now uses an aspect to move the cube up and down.

## Prefabs sample

The sample contains a single non-rendered entity with a `Spawner` component that references a cube prefab.

At runtime, the `SpawnSystem` spawns many instances of the cube prefab and places the instances at random positions. The `RotationSpeedSystem` makes the cubes rotate and fall. The `FallAndDestroySystem` destroys cubes when they fall below y coord 0. Once all cubes are destroyed, `SpawnSystem` will spawn more cubes.

## IJobChunk sample

This sample is like "IJobEntity", but it uses an `IJobChunk` instead of `IJobEntity`. Compared to `IJobEntity`, `IJobChunk` requires more boilerplate, but it provides more explicit control in some use cases.

## Reparenting sample

At regular intervals, the smaller cubes are parented and un-parented from the large rotating cube.

## EnableableComponents sample

The `EnabelableComponent` state of the rotating cubes are toggled at regular intervals, causing them to start and stop rotating.

## GameObject sync sample

This sample contains an entity with a transform that rotates, but the entity itself is not rendered. Instead, the entity syncs its transform with a rendered GameObject. A UI checkbox toggles the rotation on and off.
