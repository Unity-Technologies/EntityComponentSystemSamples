# RotationExample.unity

![](https://media.giphy.com/media/3o7WIPjJUcuIEze5Ww/giphy.gif)

## Basic description

In this example you will find:

1. Cubes are spawned randomly in a circle.
2. A sphere moves around that circle.
3. When the sphere intersects a cube, the cube rotates at a fixed rate about the y-axis.
4. When the sphere stops intersecting a cube, the cube's rotation decays at a fixed rate.

## What this example demonstrates

This examples shows you:

1. Spawning pure ECS Entities/components (not __GameObjects__)
2. Updating positions
3. Initializing positions from GameObject transform
3. Updating rotations
4. Rendering instanced models based on a generated matrix
5. Simple example of updating __ComponentData__ based on a moving sphere

## Spawn cubes in a circle

![](https://i.imgur.com/xGoyVjL.png)

Select __Create Empty__ GameObject in the Scene and name it "RotatingCubeSpawner".

![](https://i.imgur.com/GlQ7sMB.png)

Add these components to RotatingCubeSpawner:

1. [__UnityEngine.ECS.SpawnerShim/SpawnRandomCircleComponent__](../../ECSJobDemos/Assets/GameCode/SpawnerShim/SpawnRandomCircleComponent.cs)
2. [__Unity.Transforms/PositionComponent__](../../ECSJobDemos/Packages/com.unity.entities/Unity.Transforms/PositionComponent.cs)
3. [__Unity.Transforms/CopyInitialTransformFromGameObjectComponent__](../../ECSJobDemos/Packages/com.unity.entities/Unity.Transforms.Hybrid/CopyInitialTransformFromGameObjectComponent.cs)

Set the properties of __SpawnRandomCircleComponent__ to:

1. __Prefab__: [*Assets/SampleAssets/TestRotatingCube.prefab*](../../ECSJobDemos/Assets/SampleAssets/TestRotatingCube.prefab) 
This is a prefab container which contains the components for the each Entity that will be spawned. 
2. __Radius__: 25. 
Spawn entities 25m from the center of the circle.
3. __Count__: 100.
Spawn 100 entities.

The PositionComponent specifies that the entity that is created from the RotatingCubeSpawner GameObject has a position in the ECS. That position is used as the center of the circle for spawning. (Required)

The CopyInitialTransformFromGameObjectComponent specifies that **only** the initial value for PositionComponent in ECS will be copied from the GameObject's Transform. 

## Move sphere about same circle and reset rotations when intersecting cubes

![](https://i.imgur.com/GyBUpSo.png)

Select __Create Empty__ GameObject in the scene and name it "TestResetRotationSphere".

![](https://i.imgur.com/7WmSLyN.png)

Add these components to TestResetRotationSphere:

1. [__Unity.Transforms/PositionComponent__](../../ECSJobDemos/Packages/com.unity.entities/Unity.Transforms/PositionComponent.cs)
2. [__Unity.Transforms/CopyInitialTransformFromGameObjectComponent__](../../ECSJobDemos/Packages/com.unity.entities/Unity.Transforms.Hybrid/CopyInitialTransformFromGameObjectComponent.cs)
3. [__Unity.Transforms/TransformMatrixComponent__](../../ECSJobDemos/Packages/com.unity.entities/Unity.Transforms/TransformMatrixComponent.cs)
4. [__Unity.Rendering/MeshInstanceRendererComponent__](../../ECSJobDemos/Packages/com.unity.entities/Unity.Rendering.Hybrid/MeshInstanceRendererComponent.cs)
5. [__UnityEngine.ECS.SimpleMovement/MoveSpeedComponent__](../../ECSJobDemos/Assets/GameCode/SimpleMovement/MoveSpeedComponent.cs)
6. [__UnityEngine.ECS.SimpleMovement/MoveAlongCircleComponent__](../../ECSJobDemos/Assets/GameCode/SimpleMovement/MoveAlongCircleComponent.cs)
7. [__UnityEngine.ECS.SimpleRotation/RotationSpeedResetSphereComponent__](../../ECSJobDemos/Assets/GameCode/SimpleRotation/RotationSpeedResetSphereComponent.cs)

Like the RotatingCubeSpawner, the PositionComponent specifies that the Entity that is created from the TestResetRotationSphere GameObject has a position in ECS and the CopyInitialTransformFromGameObjectComponent specifies that **only** the initial value for PositionComponent in ECS will be copied from the GameObject's Transform. 

The TransformMatrixComponent specifies that a 4x4 matrix should be stored. That matrix is updated automatically based on changes to the PositionComponent.

Set the properties of the MeshInstanceRendererComponent:

1. __Mesh__: Sphere
2. __Material__: InstanceMat

Assign a Material that has GPU Instancing enabled.

This component specifies that this Mesh/Material combination should be rendered with the corresponding TransformMatrix (required).

Set the properties of the MoveSpeedComponent:

1. __Speed__: 1

This component requests that if another component is moving the PositionComponent it should respect this value and move the position at the constant speed specified.

Set the properties of the MoveAlongCircleComponent:

1. __Center__: 0,0,0
2. __Radius__: 25

The center and radius correspond to the circle of Entities that is being spawned by RotatingCubeSpawner.

This component will update the corresponding PositionComponent at the rate specified by MoveSpeedComponent in radians per second.

Set the properties of the RotationSpeedResetSphereComponent:

1. __Speed__: 4 (radians per second)
2. __Radius__: 2 (meters)

This component specifies that if any other PositionComponent is within the sphere defined by the PositionComponent on this Entity and the radius, the TransformRotationComponent on that Entity should be set to speed, if it exists.


















