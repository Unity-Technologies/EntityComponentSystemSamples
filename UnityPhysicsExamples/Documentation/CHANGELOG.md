## [Unity.Physics 0.1.0] - 2019-05-31

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


## [Unity.Physics 0.0.2] - 2019-04-08

### Changes

* Character controller now has a max slope constraint to avoid climbing slopes that are too steep.
* Character controller now does another query to check if position returned by the solver can be reached.

## [Unity.Physics 0.0.1] - 2019-03-12

* Initial package version
