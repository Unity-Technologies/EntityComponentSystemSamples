# Release Notes
All notable changes to this project will be documented in this file.

## [Physics Samples Project for 1.3.0]
### Changes
* Added demo scene `Modify Collider Geometry.unity` demonstrating runtime collider deformations. In the demo, the geometry of various types of colliders are modified via the new `ColliderBakeTransformAuthoring` component, which lets you create cyclic animations during which a game object's collider and its visuals are deformed over time. This is achieved through use of the `Collider.BakeTransform` function, which applies an affine transformation to a collider, correspondingly rotating, translating, scaling and shearing its geometry in the process. 
* Added support for spring and damping for motors in the custom authoring components. In the motor demos, these fields are used now.
* Demo scenes with motors now have examples of how to author using built-in components or custom components

### Fixes
* Modify Runtime - Collision Filter demo: fixed a bug where the render mesh of the cubes was being incorrectly changed to a sphere.
* Fixed a memory leak in the ColliderBakeTransformSystem caused by the subscene being disposed before the OnDestroy could properly dispose a collider blob

## [Physics Samples Project for 1.2.0]
### Changes
* Reorganized the samples, with cleanup of various samples' code and scenes.
* Added demo scene `Modify Collider Geometry.unity` demonstrating runtime collider deformations. In the demo, the geometry of various types of colliders are modified via the new `ColliderBakeTransformAuthoring` component, which lets you create cyclic animations during which a game object's collider and its visuals are deformed over time. This is achieved through use of the `Collider.BakeTransform` function, which applies an affine transformation to a collider, correspondingly rotating, translating, scaling and shearing its geometry in the process. 
* Added support for spring and damping for motors in the custom authoring components. In the motor demos, these fields are used now.

## [Physics Samples Project for 1.1.0]
### Changes
* Added demo scene `4e. Single Ragdoll` with a GameObject ragdoll created using the built-in physics [Ragdoll Wizard](https://docs.unity3d.com/Manual/wizard-RagdollWizard.html) and simulated using Unity Physics for DOTS on one side and the built-in GameObject physics on the other.
* Added demo scene `5m. Collider Modifications` to show how to create mesh colliders during runtime
* Added new API for BlobAssetReference MeshCollider.Create(UnityEngine.Mesh, CollisionFilter, Material) in `Physics_MeshCollider.cs`
* Added new API for BlobAssetReference MeshCollider.Create(UnityEngine.Mesh.MeshData, CollisionFilter, Material) in `Physics_MeshCollider.cs`
* Added new API for BlobAssetReference MeshCollider.Create(UnityEngine.Mesh.MeshDataArray, CollisionFilter, Material) in `Physics_MeshCollider.cs`
* Renamed the `4d. Ragdoll` scene to `4d. Ragdolls` for clarity.
* The `Mouse Hover Authoring` component was improved and now works also when hovering over physics entities whose rendering components are located in a child entity. Consequently, the previously disabled `Mouse Hover Authoring` component has now been enabled in the `4d. Ragdolls` scene.
* Added new physics component: Force Unique Collider Authoring to allow built-in colliders to be made unique during baking
* Updated the `5b. Change Box Collider Size` scene to use the new Physics Force Unique Collider component
* The `5. Modify` section now has a new section called `5g. Runtime Collider Modifications`.
* A new demo has been added: `5g1. Change Collider Material - Bouncy Boxes`. This demo instantiates prefabs containing unique colliders and modifies collider blob data during runtime
* The previously named `5g. Change Collider Filter` demo has been renamed to `5g2. Unique Collider Blob Sharing`. The focus of this demo is how collider data is shared, rather than how the `CollisionFilter` is modified.
* The `5g2. Unique Collider Blob Sharing` demo uses the new `MakeUnique` method in `SpawnExplosionAuthoring.cs`. The manual management of new colliders is no longer necessary.
* Moved the `5m. Collider Modification` demo to the `5g. Runtime Collider Modifications` section and renamed it to `5g3. Runtime Collider Creation`
* Extended the `Motion Properties - Mass` demo with an additional seesaw created using built-in physics authoring components (in grey) alongside the already existing
seesaw that uses custom physics authoring components (in yellow).
* A new demo has been added: `5g4. Runtime Collision Filter Modification`. This demo shows how to modify a CollisionFilter during runtime.

### Fixes
* Improved system start-up efficiency by not initializing systems that aren't required in the following files:
  * `Tests/Animation/Scripts/AnimationPhysicsSystem.cs`
  * `Tests/MultipleWorlds/ClientServer/Scripts/ServerPhysicsSystem.cs`
  * `Tests/MultipleWorlds/CustomPhysicsGroup/MultiWorldMousePicker.cs`
  * `Tests/MultipleWorlds/CustomPhysicsGroup/UserWorldCreator.cs`
* `Tests/SamplesTest/CreatePhysicsPerformanceTests.unity`: Fixed an index out of range bug in the OnUpdate method of the system
* Fixed bug in demo scene 5c. Change Collider Type where the collider type wasn't being updated'
* Fixed bug in `QueryTester.cs`, which is used in demos 3a. All Hits Distance Test / 3b. Cast Test / 3c. Closest Hit Distance Test, where debug draw lines were not drawing the results of the queries. Any system using drawing features of the PhysicsDebugDisplaySystem must update this system within the PhysicsDebugDisplayGroup.

## [Physics Samples Project for 1.0.0]

* Added automatic import of _Custom Physics Authoring_ experience: <br> The custom physics authoring experience, built around the `PhysicsBodyAuthoring` and `PhysicsShapeAuthoring` components, is no longer embedded in the Unity Physics package API. Instead it is provided as a Unity Physics package sample called _Custom Physics Authoring_. This sample is now automatically imported into the PhysicsSamples project under `Assets/Samples/Unity Physics/<package version>/Custom Physics Authoring`. If you want to update the Unity Physics package in the PhysicsSamples project make sure to import the new version of this sample using the Package Manager under the _Samples_ tab after the update and then delete the previous version of this sample from the project. 

## [Physics Samples Project for 1.0.0-pre.15]
### Changes
* Added demo scenes for new motor joints under `Assets/Demos/4. Joints/4c. Motors`.
* Added `SimulationValidationAuthoring` component for physics behavior validation testing, which can be used to confirm that joints behave as expected and that rigid bodies are at rest if desired.
* Added simulation validation to the following PlayMode tests in the `Assets` folder:
  * `Tests/Stacking/BasicStacks.unity`
  * `Tests/JointTest/LimitedHinge/LimitedHingeSub.unity`
  * `Tests/JointTest/Prismatic.unity`
  * `Tests/JointTest/Hinge.unity`
  * `Demos/4. Joints/4a. Joints Parade/4a. Joints Parade SubScene.unity`
  * `Demos/4. Joints/4c. Motors/4c3. Linear Velocity Motor.unity`
  * `Demos/4. Joints/4c. Motors/4c4. Angular Velocity Motor.unity`
* Changed base class of systems from SystemBase to ISystem
### Fixes
  * `Demos/5. Modify/5f. Change Surface Velocity.unity` fixed a dependency in the DisplayConveyorBeltJob
### Known Issues

## [Physics Samples Project for 0.51.0-preview.32] - 2022-06-30
### Changes
* Packages:
  * Updated com.unity.physics from `0.50.0-preview.24` to `0.51.0-preview.32`  
  * Updated com.havok.physics from `0.50.0-preview.24` to `0.51.0-preview.32`  
  * Updated com.unity.collections from `1.2.3-pre.24` to `1.3.1`
  * Updated com.unity.entities from `0.50.0-preview.24` to `0.51.0-preview.32`
  * Updated com.unity.jobs from `0.50.0-preview.8` to `0.51.0-preview.32`
  * Updated com.unity.rendering.hybrid from `0.50.0-preview.24` to `0.51.0-preview.32`
  * Updated com.unity.render-pipelines.universal from `10.8.1` to `12.1.7`
* Editor:
  * Updated Editor version from `2020.3.30f1` to `2021.3.4f1`  
* Documentation:
  * Updated Documentation folder has been renamed to READMEimages and Samples.md has been renamed to README.md.  
* Shader PhysicsStatic material has been disabled due to some rendering errors with `2021.3.4f1` a new material substitutes the shader while the errors gets fixed.
* Updated NativeHashMap to NativeParallelHashMap.

### Fixes
### Known Issues

## [Physics Samples Project for 0.50.0-preview.24] - 2022-03-25
### Changes
* Project:
  * Updated project name from UnityPhysicsSamples to PhysicsSamples.   
  * add urp to the project.
  * upgrade shaders so they are urp compatible.
  * cleanup Shaders/PhysicsStaticInputs.hlsl and Shaders/PhysicsStatic.shader + add materials.
* Packages:
  * Updated com.unity.physics to `0.50.0-preview.24`  
  * Updated com.havok.physics  to `0.50.0-preview.24`  
  * Updated com.unity.collections to `1.2.3-pre.24`
  * Updated com.unity.entities to `0.50.0-preview.24`
  * Updated com.unity.jobs to `0.50.0-preview.8`
  * Updated com.unity.rendering.hybrid to `0.50.0-preview.24`
  * Updated com.unity.render-pipelines.universal to `10.8.1` 

### Fixes
  * fixed materials for all demos
  * fixed up ragdoll parts scale based on collider mesh bounds
  * fixed material colors
  
### Known Issues

## [Physics Samples Project for 0.10.0-preview] - 2021-12-31
### Changes
* Added `ImmediatePhysicsWorldStepper` utility class for running physics simulation immediately on the current thread.
* Changed `ProjectIntoFutureOnCueSystem` in Pool demo to use `ImmediatePhysicsWorldStepper`.
* Added Assets/Tests/MultipleWorlds/Animation/Animation scene that shows how you can use a separate `PhysicsWorld` to simulate a small number of non-critical bodies for animation purposes (character's ponytail and scabbard). `DriveAnimationBodySystem` syncs critical bodies between default and side physics world (character's head and torso) and prepares the data for non-critical bodies (position and velocity correction for ponytail and scabbard). `AnimationPhysicsSystem` performs single-threaded `PhysicsWorld` building, simulation and export to ECS components. The demo shows 2 variants of physically animated ponytail: with position and velocity corrections (green) and without them (blue).
* Added Assets/Tests/MultipleWorlds/ClientServer/ClientServer scene as a simplified client-server sample, with game-critical ghost bodies (exist on both server and client) and client-only bodies that are simulated only locally to add to game's visual appeal.
Bodies in server physics world are simulated first (would be predicted based on server data in real use-cases) and then mirrored into the default physics world. Default physics world contains ghost and client-only bodies, where ghost bodies are driven by their server counterparts (game objects are linked via Server Entity setting in Drive Ghost Body Authoring script). `DriveGhostBodySystem` drives ghost bodies by position or velocity, the ratio (position vs velocity) is determined by First Order Gain setting in Drive Ghost Body Authoring script on the client ghost body game object. `ServerPhysicsSystem` is the key system that builds, simulates and exports server physics world. 
The demo shows different kinematic bars (client-only, dynamic and kinematic ghost) rotating and pushing various speheres around (client-only, dynamic and kinematic ghosts driven by position, velocity and both equally).
### Fixes
### Known Issues

## [Physics Samples Project for 0.9.0-preview.4] - 2021-05-19
### Changes
* Dependencies
  * Updated Data Flow Graph to `0.21.0-preview.1`
  * Updated IDE Visual Studio to `2.0.7`
  * Updated Hybrid Renderer `0.13.0-preview.30`
### Fixes
### Known Issues

## [Physics Samples Project for 0.8.0-preview] - 2021-03-26

### Changes
* Dependencies
  * Updated DOTS Editor from `0.13.0-preview` to `0.14.0-preview.1`
  * Updated Hybrid Renderer from `0.12.0-preview.42` to `0.13.0-preview.17`
  * Updated Burst from `1.4.4` to `1.5.0`

* Refactored the `CollisionEvent` and `TriggerEvent` demos to make the Stateful events clearer.
  * To add stateful events to your own project copy all scripts from the [Stateful](Assets/Demos/2.%20Setup/2d.%20Events/Scripts/Stateful) folder.
  * Then, if you want to have collision events:
    1. Use the **'Collide Raise Collision Events'** option of the **'Collision Response'** property of a `PhysicsShapeAuthoring` component, and
    2. Add a `StatefulCollisionEventBufferAuthoring` component to that entity (and select if details should be calculated or not)
    3. At runtime, read from the dynamic buffer of `StatefulCollisionEvent`s
  * Alternatively, if you want to have trigger events:
    1. Use the **'Raise Trigger Events'** option of the **'Collision Response'** property of a `PhysicsShapeAuthoring` component, and
    2. Add a `StatefulTriggerEventBufferAuthoring` component to that entity
    3. At runtime, read from the dynamic buffer of `StatefulTriggerEvent`s

## [Physics Samples Project for 0.7.0-preview.3] - 2021-02-24

### Changes

* Dependencies
  * Updated Data Flow Graph from `0.18.0-preview.3` to `0.20.0-preview.4`
  * Updated DOTS Editor from `0.12.0-preview.4` to `0.13.0-preview`
  * Updated Hybrid Renderer from `0.10.0-preview.21` to `0.12.0-preview.42`
  * Updated Coding from `0.1.0-preview.17` to `0.1.0-preview.20`
  
* Added a new `MouseHoverAuthoring` script to help highlight the extra `CompoundCollider.Child.Entity` field and `PhysicsRenderEntity` component data. Check out the simple `1b. Representations` and more complex `2a2. Collider Parade - Advanced` demos scene for example of setup variations. 
* Added `Tests/VelocityClipping/VelocityClippingStacking` demo to showcase a simple replacement for the removed `SimulationCallbacks.Phase.PostSolveJacobians`. This demo does a really simple velocity clipping and the point is not to showcase stacking (which is covered with `Tests/Stacking` demos) but instead to show how to achieve the effect similar to previous callback without using the callback itself.
* Added `Tests/SystemScheduling/SchedulingSample` which shows how jobs that interact with physics runtime data (stored in PhysicsWorld) should be scheduled.

### Fixes

* Fixed a bug where duplicate joint Entities were created when multiple joint components are added to the same game object.

### Known Issues

## [Physics Samples Project for 0.6.0-preview.3] - 2021-01-18

### Changes
* Dependencies
  * Updated Data Flow Graph from `0.18.0-preview.3` to `0.19.0-preview.5`
  * Updated DOTS Editor from `0.9.0-preview.1` to `0.12.0-preview.4`
  * Updated Hybrid Renderer from `0.10.0-preview.21` to `0.11.0-preview.40`
* Added `5g. Change Collider Filter` sample showing explosion use case and the importance of collision filters in that case. Setting the force to 0 in `SpawnExplosionAuthoring` causes a performance spike because when the filters get set to default, a lot of bodies are pentetrating each other.

### Fixes

### Known Issues

## [Physics Samples Project for 0.5.1-preview.2] - 2020-10-14

### Changes

* Dependencies
  * Updated Hybrid Renderer from `0.7.0-preview.24` to `0.10.0-preview.21`
  * Updated Data Flow Graph from `0.16.0-preview.3` to `0.18.0-preview.1`
* All demos now have a target frame rate of 60HZ.

### Fixes

### Known Issues

## [Physics Samples Project for 0.5.0-preview] - 2020-09-15

### Changes

* Moved `LimitDOF` joint into core Unity Physics package

### Fixes

* Fixed issue with the `FreeHingeJoint.Create` authoring script not setting the `BodyFrame.PerpendicularAxis`

### Known Issues

* `RaycastCar` demo has issues that sometimes produce NaN values.

## [Physics Samples Project for 0.4.1-preview] - 2020-07-28

### Changes

* Added simple `1b. Respresentations` sample highlighting graphical and physical representations of the world.
* Added simple `1c. Conversion` sample, along with worked examples moving from GameObjects (Data & Logic) to DOTS.
    * New `1c1. GameObjects GravityWell` sample - showing Data conversion working but not Logic conversion.
    * New `1c2. Covertible GravityWell` sample - showing Data & Logic conversion.
    * New `1c3. DOTS GravityWell` sample - showing same scene without GameObject conversion.
* Updated `2a2. Collider Parade - Advanced` sample with extra setup samples involving multiple graphics mesh and/or multiple physics shapes.
* Updated `4. Joints\Ragdoll` demo to use new ragdoll joint interface and allow modification of joint limits.
* Added `5d. Change Velocity` demo showing a local velocity change.
* Added `5f. Change Surface Velocity` sample highlighting a conveyor belt use case with no moving parts.

## [Physics Samples Project for 0.4.0-preview.5] - 2020-06-18

### Changes

* Updated the following packages:
    * Added Data Flow Graph `0.14.0-preview.2`
    * Added DOTS Editor `0.7.0-preview.1`
    * Hybrid Renderer from `0.4.0-preview.8` to `0.5.1-preview.18`
* The `PrismaticJoint` example no longer includes `MinDistanceFromAxis` or `MaxDistanceFromAxis` parameters.
* The `RagdollJoint` example now takes perpendicular limits in the range of (-90, 90) rather than (0, 180).
* Added `5d. Kinematic Motion` to illustrate different ways of moving kinematic bodies.
* Improved usability of trigger events and collision events:
	* Events (StatefulTriggerEvent and StatefulCollisionEvent) have states indicating overlap or colliding state of two bodies:
		* Enter - the bodies didn't overlap or collide in the previous frame, and they do in the current frame
		* Stay - the bodies did overlap or collide in the previous frame, and they do in the current frame
		* Exit - the bodies did overlap or collide in the previous frame, and they do not in the current frame
	* Events (StatefulTriggerEvent and StatefulCollisionEvent) are stored in DynamicBuffers of entities that raise them
	* Reworked following demos to demonstrate new trigger event approach:
		* `2d1a. Triggers - Change Material`
		* `2d1b. Triggers - Portals`
		* `2d1c. Triggers - Force Field`
	* `2d2a. Collision Events - Event States` added to demonstrate new collision event approach.
* Exposed trigger events and collision events of CharacterController
* CharacterController body is now using a `CollisionResponse.None` collision response on its body to avoid reporting duplicated collision/trigger events coming from the physics engine.
* `Demos/2. Setup/2b. Motion Properties/2b1. Motion Properties - Mass`, `Tests/Pyramids` and a group of demos under `Tests/Stacking` now showcase new solver stabilization features/strengths and weaknesses/trade-offs.

## [Physics Samples Project for 0.3.2-preview] - 2020-04-16

### Changes

* Updated the following packages:
    * Removed DOTS Editor
    * Hybrid Renderer from `0.3.4-preview.24` to `0.4.0-preview.8`
    * Input System from `1.0.0-preview.5` to `1.0.0-preview.6`
* Fixed character controller tunnelling issue.
* Made standalone player quit (with exit code 1) if an exception is caught in BasePhysicsDemo or derived classes.

## [Physics Samples Project for 0.3.1-preview] - 2020-03-19

### Changes
* Fixed a potential character controller tunnelling issue.
* Removed the Lightweight RP package.

## [Physics Samples Project for 0.3.0-preview] - 2020-03-12

### Changes

* Joint samples now correctly add new joint entity to prefab's linked entity group so they will be instantiated along with the rest of a prefab.
* Renamed `StiffSpringJoint` to `LimitedDistanceJoint` to reflect changes in API.
* Updated the following packages
    * Input System from `0.9.6-preview` to `1.0.0-preview.5`
    * Lightweight RP from `7.1.6` to `7.1.7`
    * DOTS Editor from `0.2.0-preview` to `0.3.0-preview`
    * Hybrid Renderer from `0.3.3-preview.11` to `0.3.4-preview.24`
* Character controller improvements
    * `CharacterControllerUtilities.CheckSupport()` now uses a collider cast
    * Character now doesn't collide with triggers, but raises trigger events instead
    * Fixed the support check issue with slopes equal to MaxSlope
    * Fixed the returned support state when there are no supporting planes

## [Physics Samples Project for 0.2.5-preview] - 2019-12-04

### Changes
* Added `CharacterControllerAuthoring.MaxMovementSpeed` to avoid large velocities coming from penetration recovery.
* Improved the behavior of character controller walking on multiple objects or mesh triangles:
    * Fixed the problems with losing support state
    * Fixed the problems with being unstable due to interaction with steep slopes
    * Improved penetration recovery
* Character controller is now using native lists instead of native arrays for constraints and query hits to avoid large preallocations.
* Planet gravity sample now correctly randomizes mass of orbiting bodies.

## [Physics Samples Project for 0.2.4-preview] - 2019-09-19

### Changes

* Fixed the issue with collider cast in character controller assuming ordered hits, potentially tunneling through objects.
* When opening the Project with Unity `2019.3` below `0b5` it might be required to update the *Lightweight RP* package (com.unity.render-pipelines.lightweight) to version `7.0.1`  and reimport the `Assets/Common` folder.


## [Physics Samples Project for 0.2.2-preview] - 2019-09-06

### Changes

* Character controller changes include:
    - Character controller demo scenes merged into single scene.
    - Character controller can now use any collision filter, as opposed to previously being forced to set it to 'Nothing'.
    - Default max slope to climb is now 60 degrees.
    - Character controller can now use any collider, instead of previously being forced to use capsule.
    - Fixed a race condition problem with scheduling character controller jobs when their data is in multiple chunks.
    - Simple collisions between characters are now supported (not going through each other).
    - KeepDistance with a default of 2cm has been added to avoid character getting stuck in narrow passages.
    - User input is now projected onto supporting surface, incorporating its velocity into input.
    - CharacterControllerUtilities.CheckSupport() has been deprecated. Use the new CheckSupport() method that outputs the surface info.
    - CharacterControllerUtilities.CollideAndIntegrate() has been deprecated. Use the new CollideAndIntegrate() method that takes numConstraints as input.
* Menu loader scene now supports keyboard and controller input. Controller input now maps correctly across platforms. Controls have been updated to the following:
    * Keyboard/Mouse
        * UI:
            * Arrow keys to navigate
            * [Return] to select
        * Character Controller
            - [WASD] to move
            - Mouse look
            - [Space] to jump
            - [LMB/Ctrl] to shoot
        * Vehicle
            - [WS] to accelerate/decelerate
            - [AD] to steer
            - [Left/Right] arrows to look
            - [Left/RightBracket] to switch cars
    * Controller:
        * UI
            * D-Pad to navigate
            * [West] to select
        * Character Controller
            - Left Stick to move
            - Right Stick look
            - [South] to jump
            - [Right Trigger] to shoot
        * Vehicle
            - [Left/Right Trigger] to accelerate/decelerate
            - Left Stick to steer
            - Right Stick arrows to look
            - [Left/Right Bumper] to switch cars

### Opening the Project with different Unity Editor versions

* To open the project with Unity `2019.3.0b1` or later, it is currently required to upgrade the following packages:
   - *Hybrid Renderer* (com.unity.rendering.hybrid) has to be upgraded to `0.1.1-preview`
   - *Lightweight RP* (com.unity.render-pipelines.lightweight) has to be updated to version `7.0.1`
   - A reimport of the materials might be required when this is done while the Unity Editor is opened.


## [Physics Samples Project for 0.2.0-preview] - 2019-07-18

### Changes

* `5a. Change Motion Type` demo illustrating how to swap the motion type of a rigid body.
* `5b. Change Collider Size` demo shows how to dynamically resize a sphere collider.
* `5c. Change Collider Type` demo shows a body's collider changing between a cube and a sphere.
* Removed `4c. Newton's Cradle` demo
* Removed `4d. Abacus` demo
* Added a loader scene to more easily test all examples on a device.


## [Physics Samples Project for 0.1.0-preview] - 2019-05-31

### Changes

* `1. Hello World` now uses convex hulls for letters.
* `2b1. Motion Properties - Mass` shows a number of seesaws in anticipation of future stacking improvements.
* `2b6. Motion Properties - Inertia Tensor` adds an example of infinite mass dynamic objects - i.e lock rotations.
* `2c2. Material Properties - Restitution` shows the changes to the restitution model, with complex bodies bouncing more with high restitution values.
* `2c3. Material Properties - Collision Filters` shows the filtering setup between potentially colliding objects
* `2d1. Events - Triggers` shows a simple trigger use case that changes a bodies gravity fractor when inside the volume.
    - The more advanced trigger use cases capture state such as Entering, Overlapping and Leaving a trigger volume.
    - `2d1. Triggers - Change Material` shows set of triggers that change a bodies material when they enter the trigger volume.
    - `2d2. Triggers - Portals` shows set of triggers that transform a body to a new location and orientation while preserving local velocity.
    - `2d3. Triggers - Force Field` shows a trigger volume acting and a tornado moving through the world.
* `2d2. Events - Contacts` shows a simple collision event used to apply an impulse to any body colliding with the tagged 'repulsor' body.
* `4a. Joints Parade` demo shows the Prismatic Joint now locks rotation.
* `4b. Limit DOF` demo shows the new Limit DOF Joint, locking body rotation and translation to various axes.
* `4c. Newton's Cradle` demo shows how to cheat collision response to create a desktop toy.
* `4d. Abacus` shows a desktop toy as an extreme test of the Limit DOF Joint.
* `5. Modify` demos have been changed to use the new jobify approach to modifying simulation data.

* Mouse Pick Behavior now ignores Static and Trigger bodies, illustrating a custom hit collector.
* Joint Editors now lock the connected body offset to the local offsets, as suggested by the UI.
* Debug Display changes include:
    - Colliders are displayed as solid.
    - Collider Edges can be display independent of solid Colliders.
    - Trigger events draw connecting line between overlapping bodies.
    - Collision events draw impulse half-way between colliding bodies.

* Various tests have been added to confirm API changes and as comparisons for WIP and the upcoming Havok Physics release.


## [Physics Samples Project for 0.0.2-preview] - 2019-04-08

### Changes

* Character controller now has a max slope constraint to avoid climbing slopes that are too steep.
* Character controller now does another query to check if position returned by the solver can be reached.


## [Physics Samples Project for 0.0.1-preview] - 2019-03-12

* Initial package version and sample project.
