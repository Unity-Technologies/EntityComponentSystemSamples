## [Samples Project for 0.2.5-preview] - 2019-12-04

### Changes
* Added `CharacterControllerAuthoring.MaxMovementSpeed` to avoid large velocities coming from penetration recovery.
* Improved the behavior of character controller walking on multiple objects or mesh triangles:
    * Fixed the problems with losing support state
    * Fixed the problems with being unstable due to interaction with steep slopes
    * Improved penetration recovery
* Character controller is now using native lists instead of native arrays for constraints and query hits to avoid large preallocations.
* Planet gravity sample now correctly randomizes mass of orbiting bodies.

## [Samples Project for 0.2.4-preview] - 2019-09-19

### Changes

* Fixed the issue with collider cast in character controller assuming ordered hits, potentially tunneling through objects.
* When opening the Project with Unity `2019.3` below `0b5` it might be required to update the *Lightweight RP* package (com.unity.render-pipelines.lightweight) to version `7.0.1`  and reimport the `Assets/Common` folder.


## [Samples Project for 0.2.2-preview] - 2019-09-06

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


## [Samples Project for 0.2.0-preview] - 2019-07-18

### Changes

* `5a. Change Motion Type` demo illustrating how to swap the motion type of a rigid body.
* `5b. Change Collider Size` demo shows how to dynamically resize a sphere collider.
* `5c. Change Collider Type` demo shows a body's collider changing between a cube and a sphere.
* Removed `4c. Newton's Cradle` demo
* Removed `4d. Abacus` demo
* Added a loader scene to more easily test all examples on a device.


## [Samples Project for 0.1.0-preview] - 2019-05-31

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


## [Samples Project for 0.0.2-preview] - 2019-04-08

### Changes

* Character controller now has a max slope constraint to avoid climbing slopes that are too steep.
* Character controller now does another query to check if position returned by the solver can be reached.


## [Samples Project for 0.0.1-preview] - 2019-03-12

* Initial package version and sample project.
