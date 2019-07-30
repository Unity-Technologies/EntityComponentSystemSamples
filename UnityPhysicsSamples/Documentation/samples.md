# Unity Physics Samples

## Controls 

In the *Game* window:

- Mouse spring : Click and drag with left mouse
- Camera rotate : Click and drag with right mouse
- Camera move : W,A,S,D keys

## Debug Display

A lot of the demos display extra information as debug display gizmos in the Editor, for example the Query demos (ray cast, distance cast, etc). This display for these gizmos is in the *Scene* not the *Game* window. So keep an eye on both if in doubt:

![Game window](images/LookInSceneWindow.png)

## Scene List

| Category | Scene | Description | Level | |
| --- | --- | --- | :---: | --- |
| Hello World | <sub>1. Hello World.unity</sub> | Introductory scene for rigid body setup | Introductory | ![Demo image](images/hello_world.gif) |
| Setup | <sub>2a1. Collider Parade.unity</sub> | Demo showing various shapes for collision detection | Introductory | ![Demo image](images/collider_parade.png) |
| Setup | <sub>2b1. Motion Properties - Mass.unity</sub> | Demo showing how to explicitly set mass properties | Introductory | ![Demo image](images/motion_mass.png) |
| Setup | <sub>2b2. Motion Properties - Velocity.unity</sub> | Setting initial linear and angular velocities | Introductory |  ![Demo Image](images/motion_velocity.gif) |
| Setup | <sub>2b3. Motion Properties - Damping.unity</sub> | Demo showing the effect of linear and angular damping | Introductory |  ![Demo Image](images/motion_damping.gif) |
| Setup | <sub>2b4. Motion Properties - Gravity Factor.unity</sub> | Demo showing the effect of per body gravity multipliers | Introductory |  ![Demo Image](images/motion_gravity.gif) |
| Setup | <sub>2b5. Motion Properties - Center of Mass.unity</sub> | Demo showing the effect of overriding center of mass and inertia tensor | Introductory | ![Demo Image](images/motion_mass_override.gif) |
| Setup | <sub>2c1. Material Properties - Friction.unity</sub> | Showing effect of different friction material values | Introductory | ![Demo Image](images/material_friction.gif) |
| Setup | <sub>2c2. Material Properties - Restitution.unity</sub> | Showing effect of different restitution values | Introductory | ![Demo Image](images/material_restitution.gif) |
| Setup | <sub>ShapesSample.unity</sub> | Testing demo for colliders. Turn on / off spawners to test scalability | Introductory | ![Demo Image](images/shapes_sample.png) |
| Query | <sub>AllHitsDistanceTest.unity</sub> | Demo showing results of distance queries between multiple colliders | Introductory | ![Demo Image](images/closest_points_all_hits.gif) |
| Query | <sub>CastTest.unity</sub> | Demo showing the results of collider casting and ray casting | Introductory | ![Demo Image](images/collider_cast_queries.gif) |
| Query | <sub>ClosestHitDistanceTest.unity</sub> | Demo showing results of distance queries | Introductory | ![Demo Image](images/closest_points.gif) |
| Joints | <sub>4a. Joints Parade.unity</sub> | Demo showing a range of joint types | Introductory  | ![Ragdoll](images/joints_sample.png) |
| Joints | <sub>Ragdoll.unity</sub> | Obligatory stack of ragdolls demo | Introductory  | ![Ragdoll](images/ragdolls.gif) |
| Simulation | <sub>AddNarrowphaseContacts.unity</sub> | Dynamically add user generated contact points | Advanced | ![Modify broadphase Sample](images/add_contacts.gif) |
| Simulation | <sub>ModifyBroadphasePairs.unity</sub> | Filter out collision by explicitly deleting pairs from broad phase | Advanced | ![Modify broadphase Sample](images/modify_broadphase.png) |
| Simulation | <sub>ModifyContactJacobians.unity</sub> | Modify the results of contact generation to produce special effects | Advanced | ![Modify contacts](images/modify_contact.png) |
| Simulation | <sub>ModifyNarrowphaseContacts.unity</sub> | Add new user contacts to simulation pipeline  | Advanced | No screenshot | 
| Use Case | <sub>CharacterController.unity</sub> | User case demo showing a rudimentary FPS character controller | Intermediate | ![Character Control](images/character_control.png) |
| Use Case | <sub>Pool.unity</sub> | Demonstration of calling immediate mode physics | Intermediate | ![Immediate physics](images/immediate_physics.png) |
| Use Case | <sub>PlanetGravity.unity</sub> | Performance demo of asteroids around a planet using SP/HP | Introductory | ![Planet Gravity](images/planet_gravity.png) |
| Use Case | <sub>RaycastCar.unity</sub> | User case demo showing a set of vehicle behaviors | Intermediate | ![Vehicles](images/vehicles.png) |
