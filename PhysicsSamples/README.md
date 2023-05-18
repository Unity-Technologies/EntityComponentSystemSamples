# Unity Physics Samples

## Controls 

In the *Game* window:

- Mouse spring : Click and drag with left mouse
- Camera rotate : Click and drag with right mouse
- Camera move : W,A,S,D keys

## Debug Display

A lot of the demos display extra information as debug display gizmos in the Editor, for example the Query demos (ray cast, distance cast, etc). This display for these gizmos is in the *Scene* not the *Game* window. So keep an eye on both if in doubt:

| Scene view                                                                  | Game view                                                                   |
|-----------------------------------------------------------------------------|-----------------------------------------------------------------------------|
| ![Game window](READMEimages/closes_hit_scene_view.png)                      | ![Game window](READMEimages/closes_hit_game_view.png)                       |

## Scene List

| Category    | Scene                                                         | Description                                                         |    Level     |                                                                                  |
|-------------|---------------------------------------------------------------|---------------------------------------------------------------------|:------------:|----------------------------------------------------------------------------------|
| Hello World | <sub>1a. Hello World.unity</sub>                              | Introductory scene for rigid body setup                             | Introductory | ![Demo image](READMEimages/hello_world.gif)                                      |
| Hello World | <sub>1b. Representations.unity</sub>                          | Introductory scene for combining physics with graphics              | Introductory | ![Demo image](READMEimages/representations.gif)                                  |
| Hello World | <sub>1c. Conversion.unity</sub>                               | Introductory scene for conversions                                  | Introductory | ![Demo image](READMEimages/conversion.gif)                                       |
| Setup       | <sub>2a1. Collider Parade - Basic.unity</sub>                 | Demo showing various shapes for collision detection                 | Introductory | ![Demo image](READMEimages/collider_parade_basic.gif)                            |
| Setup       | <sub>2a2. Collision Parade - Advanced</sub>                   | Demo showing various shapes for more advanced collision detection   | Introductory | ![Demo image](READMEimages/collider_parade_advanced.gif)                         |
| Setup       | <sub>2b1. Motion Properties - Mass.unity</sub>                | Demo showing how to explicitly set mass properties                  | Introductory | ![Demo image](READMEimages/motion_properties_mass.gif)                           |
| Setup       | <sub>2b2. Motion Properties - Velocity.unity</sub>            | Setting initial linear and angular velocities                       | Introductory | ![Demo Image](READMEimages/motion_properties_velocity.gif)                       |
| Setup       | <sub>2b3. Motion Properties - Damping.unity</sub>             | Demo showing the effect of linear and angular damping               | Introductory | ![Demo Image](READMEimages/motion_properties_damping.gif)                        |
| Setup       | <sub>2b4. Motion Properties - Gravity Factor.unity</sub>      | Demo showing the effect of per body gravity multipliers             | Introductory | ![Demo Image](READMEimages/motion_properties_gravity_factor.gif)                 |
| Setup       | <sub>2b5. Motion Properties - Center of Mass.unity</sub>      | Demo showing the effect of overriding center of mass                | Introductory | ![Demo Image](READMEimages/motion_properties_center_of_mass.gif)                 |
| Setup       | <sub>2b6. Motion Properties - Inertia Tensor.unity</sub>      | Demo showing the effect of overriding inertia tensor                | Introductory | ![Demo Image](READMEimages/motion_properties_inertia_tensor.gif)                 |
| Setup       | <sub>2b7. Motion Properties - Smoothing.unity</sub>           | Demo showing the effect of interpolation and extrapolation          | Introductory | ![Demo Image](READMEimages/motion_properties_smoothing.gif)                      |
| Setup       | <sub>2c1. Material Properties - Friction.unity</sub>          | Showing effect of different friction material values                | Introductory | ![Demo Image](READMEimages/material_properties_friction.gif)                     |
| Setup       | <sub>2c2. Material Properties - Restitution.unity</sub>       | Showing effect of different restitution values                      | Introductory | ![Demo Image](READMEimages/material_properties_restitution.gif)                  |
| Setup       | <sub>2c3. Material Properties - Collision Filters.unity</sub> | Showing effect of different collision filters                       | Introductory | ![Demo Image](READMEimages/material_properties_collision_filters.gif)            |
| Setup       | <sub>2d1. Events - Triggers.unity</sub>                       | Demo demonstrating the usage of triggers                            | Introductory | ![Demo Image](READMEimages/events_triggers.gif)                                  |
| Setup       | <sub>2d2. Events - Contacts.unity</sub>                       | Showing effect of different contacts                                | Introductory | ![Demo Image](READMEimages/events_contacts.gif)                                  |
| Query       | <sub>3a. AllHitsDistanceTest.unity</sub>                      | Demo showing results of distance queries between multiple colliders | Introductory | ![Demo Image](READMEimages/all_hits_distance_test.gif)                           |
| Query       | <sub>3b. CastTest.unity</sub>                                 | Demo showing the results of collider casting and ray casting        | Introductory | ![Demo Image](READMEimages/cast_test.gif)                                        |
| Query       | <sub>3c. ClosestHitDistanceTest.unity</sub>                   | Demo showing results of distance queries                            | Introductory | ![Demo Image](READMEimages/closest_hit_distance_test.gif)                        |
| Query       | <sub>3d. Custom Collector.unity</sub>                         | Demonstration of raycast                                            | Introductory | ![Demo Image](READMEimages/custom_collector.gif)                                 |
| Joints      | <sub>4a. Joints Parade.unity</sub>                            | Demo showing a range of joint types                                 | Introductory | ![Demo Image](READMEimages/joints_parade.gif)                                    |
| Joints      | <sub>4b. Limit DOF.unity</sub>                                | Showing effect of limiting degrees of freedom                       | Introductory | ![Demo Image](READMEimages/limit_dof.gif)                                        |
| Joints      | <sub>4c1. All Motors Parade.unity</sub>                       | Demo showing different motors                                       | Introductory | ![Demo Image](READMEimages/all_motors_parade.gif)                                |
| Joints      | <sub>4c2. Position Motor.unity</sub>                          | Demo showing position motor                                         | Introductory | ![Demo Image](READMEimages/position_motor.gif)                                   |
| Joints      | <sub>4c3. Linear Velocity Motor.unity</sub>                   | Showing linear velocity motor                                       | Introductory | ![Demo Image](READMEimages/linear_velocity_motor.gif)                            |
| Joints      | <sub>4c4. Angular Velocity Motor.unity</sub>                  | Demonstrating angular velocity motor                                | Introductory | ![Demo Image](READMEimages/angular_velocity_motor.gif)                           |
| Joints      | <sub>4c5. Rotational Motor.unity</sub>                        | Demonstrating rotational motor                                      | Introductory | ![Demo Image](READMEimages/rotational_motor.gif)                                 |
| Joints      | <sub>4d. Ragdoll.unity</sub>                                  | Obligatory stack of ragdolls demo                                   | Introductory | ![Ragdoll](READMEimages/ragdoll.gif)                                             |
| Modify      | <sub>5a. Change Motion Type.unity</sub>                       | Demo showing change of motion type                                  | Introductory | ![Demo Image](READMEimages/change_motion_types.gif)                              |
| Modify      | <sub>5b. Change Box Collider Size.unity</sub>                 | Demonstrating runtime change of collider size                       | Introductory | ![Demo Image](READMEimages/change_box_collider_size.gif)                         |
| Modify      | <sub>5c. Change Collider Type.unity</sub>                     | Demonstrating change of collider type                               | Introductory | ![Demo Image](READMEimages/change_collider_type.gif)                             |
| Modify      | <sub>5d. Change Velocity.unity</sub>                          | Demo showing change of velocity                                     | Introductory | ![Demo Image](READMEimages/change_velocity.gif)                                  |
| Modify      | <sub>5e. Kinematic Motion.unity</sub>                         | Demo showing kinematic motion in combination with dynamic objects   | Introductory | ![Demo Image](READMEimages/kinematic_motion.gif)                                 |
| Modify      | <sub>5f. Change Surface Velocity.unity</sub>                  | Demo showing change of surface velocity                             | Introductory | ![Demo Image](READMEimages/change_surface_velocity.gif)                          |
| Modify      | <sub>5g. Change Collider Filter.unity</sub>                   | Demo showing effect of collider filter change                       | Introductory | ![Demo Image](READMEimages/change_collider_filter.gif)                           |
| Modify      | <sub>5h. Change Scale.unity</sub>                             | Demo showing scale change of entities                               | Introductory | ![Demo Image](READMEimages/change_scale.gif)                                     |
| Modify      | <sub>5i. Apply Impulse.unity</sub>                            | Demo showing application of impulses                                | Introductory | ![Demo Image](READMEimages/apply_impulse.gif)                                    |
| Modify      | <sub>5j. ModifyBroadphasePairs.unity</sub>                    | Filter out collision by explicitly deleting pairs from broad phase  |   Advanced   | ![Modify broadphase Sample](READMEimages/modify_broadphase_pairs.gif)            |
| Modify      | <sub>5k. ModifyContactJacobians.unity</sub>                   | Modify the results of contact generation to produce special effects |   Advanced   | ![Modify contacts](READMEimages/modify_contact_jacobians.gif)                    |
| Modify      | <sub>5l. ModifyNarrowphaseContacts.unity</sub>                | Add new user contacts to simulation pipeline                        |   Advanced   | ![Modify Narrowpahse contacts](READMEimages/modify_narrowphase_contacts.gif)     | 
| Use Case    | <sub>6a. CharacterController.unity</sub>                      | User case demo showing a rudimentary FPS character controller       | Intermediate | ![Character Control](READMEimages/character_controller.gif)                      |
| Use Case    | <sub>6b. Pool.unity</sub>                                     | Demonstration of calling immediate mode physics                     | Intermediate | ![Immediate physics](READMEimages/pool.gif)                                      |
| Use Case    | <sub>6c. PlanetGravity.unity</sub>                            | Performance demo of asteroids around a planet using SP/HP           | Introductory | ![Planet Gravity](READMEimages/planet_gravity.gif)                               |
| Use Case    | <sub>6d. RaycastCar.unity</sub>                               | User case demo showing a set of vehicle behaviors                   | Intermediate | ![Vehicles](READMEimages/raycast_car.gif)                                        |
