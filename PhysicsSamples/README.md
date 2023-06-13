# Unity Physics Samples

In the *Game* window:

- Drag objects: click and drag with left mouse
- Rotate camera: click and drag with right mouse
- Move camera: WASD keys

**NOTE:** Many of these samples display extra information as debug display gizmos in the Editor. For example, the Query samples display raycasts as debug lines that are only visible when Gizmos are enabled.


## Scene List

Scene                                                         | Description                                                         |    Level     |                                                                                  |
|---------------------------------------------------------------|-----------------------------------------------------------------|:------------:|----------------------------------------------------------------------------------|
| 1. Hello World /<br> Hello World       | Objects with convex hull colliders                              | ![Demo image](READMEimages/hello_world.gif)                                      |
| 1. Hello World /<br> Sphere and Box Colliders    | Objects with sphere and box colliders               | ![Demo image](READMEimages/representations.gif)                                  |
| 2. Gravity Well /<br> Gravity Well                | Two objects rotating around a center and smaller objects gravitating to the larger two. | ![Demo image](READMEimages/gravitywell.gif)                                       |
| 3. Collider Geometry /<br> Collision Parade - Basic                 | The basic collider shapes                  | ![Demo image](READMEimages/collider_parade_basic.gif)                            |
| 3. Collider Geometry /<br> Collision Parade - Advanced                  | Colliders of all shapes, including compound colliders    | ![Demo image](READMEimages/collider_parade_advanced.gif)                         |
| 4. Motion Properties /<br> Mass                | Overriding mass                   | ![Demo image](READMEimages/motion_properties_mass.gif)                           |
| 4. Motion Properties /<br> Velocity            | Objects that spawn with a set initial linear and angular velocity                        | ![Demo Image](READMEimages/motion_properties_velocity.gif)                       |
| 4. Motion Properties /<br> Damping             | Linear and angular damping                | ![Demo Image](READMEimages/motion_properties_damping.gif)                        |
| 4. Motion Properties /<br> Gravity Factor      | Per-body gravity multipliers              | ![Demo Image](READMEimages/motion_properties_gravity_factor.gif)                 |
| 4. Motion Properties /<br> Center of Mass      | Overriding center of mass                 | ![Demo Image](READMEimages/motion_properties_center_of_mass.gif)                 |
| 4. Motion Properties /<br> Inertia Tensor      | Overriding inertia tensor                 | ![Demo Image](READMEimages/motion_properties_inertia_tensor.gif)                 |
| 4. Motion Properties /<br> Smoothing           | Objects with either interpolation, extrapolation, or neither           | ![Demo Image](READMEimages/motion_properties_smoothing.gif)                      |
| 5. Material Properties /<br> Friction          | Objects with different friction strength sliding down a slope                 | ![Demo Image](READMEimages/material_properties_friction.gif)                     |
| 5. Material Properties /<br> Restitution       | Falling objects with different restitution (fancy word for bounciness!)                       | ![Demo Image](READMEimages/material_properties_restitution.gif)                  |
| 5. Material Properties /<br> Collision Filters | Objects only collide with other objects belonging to the matching material collision filter                        | ![Demo Image](READMEimages/material_properties_collision_filters.gif)            |
| 6. Events /<br> Collisions                   | Detecting object contact with a trigger surface  | ![Demo Image](READMEimages/collisions.gif) |
| 6. Events /<br> Contacts                    | Object repelled upon hitting a trigger volume                                 | ![Demo Image](READMEimages/events_contacts.gif)                      |
| 6. Events /<br> Triggers                       | Objects reverse gravity upon entering trigger volume                             | ![Demo Image](READMEimages/events_triggers.gif)                                  |
| 6. Events /<br> Triggers - Change Material                       | Objects change material upon entering trigger volume                             |     ![Demo Image](READMEimages/changematerial.gif)                                    |
| 6. Events /<br> Triggers - Force Field                    | Objects float upward when inside moving trigger volume                | ![Demo Image](READMEimages/forcefield.gif)                                        |
| 6. Events /<br> Triggers - Portals                   | Teleport an object from one portal to another upon contact                             | ![Demo Image](READMEimages/portal.gif)                   |
| 7. Query /<br> All Distances                     | Query for the distance between the colliders for each collider vertex  | ![Demo Image](READMEimages/all_hits_distance_test.gif)                           |
| 7. Query /<br> Closest Distance                   | Query for closest distance between two colliders                            | ![Demo Image](READMEimages/closest_hit_distance_test.gif)                        |
| 7. Query /<br> Cast                                 | Collider casting and ray casting         | ![Demo Image](READMEimages/cast_test.gif)                                        |
| 7. Query /<br> Custom Collector                         | Raycast with custom filtering                                             | ![Demo Image](READMEimages/custom_collector.gif)                                 |
| 8. Joints and Motors /<br> Joints - Parade                           | All joint types                                  | ![Demo Image](READMEimages/joints_parade.gif)                                    |
| 8. Joints and Motors /<br> Joints - Limit DOF                                | Limiting degrees of freedom                        | ![Demo Image](READMEimages/limit_dof.gif)                                        |
| 8. Joints and Motors /<br> Joints - Ragdolls                                  | Obligatory stack of ragdolls                                    | ![Ragdoll](READMEimages/ragdoll.gif)                      |
| 8. Joints and Motors /<br> Motors - Parade                       | All motor types                                        | ![Demo Image](READMEimages/all_motors_parade.gif)                                |
| 8. Joints and Motors /<br> Motors - Position                          | Position motors                                          | ![Demo Image](READMEimages/position_motor.gif)                                   |
| 8. Joints and Motors /<br> Motors - Linear Velocity                  | Linear velocity motors                                        | ![Demo Image](READMEimages/linear_velocity_motor.gif)                            |
| 8. Joints and Motors /<br> Motors - Angular Velocity              | Angular velocity motors                                 | ![Demo Image](READMEimages/angular_velocity_motor.gif)                           |
| 8. Joints and Motors /<br> Motors - Rotational                | Rotational motors                                       | ![Demo Image](READMEimages/rotational_motor.gif)                                 |
| 9. Modify /<br> Motion Type                      | Dynamically modifying motion type                                   | ![Demo Image](READMEimages/change_motion_types.gif)                              |
| 9. Modify /<br> Box Collider Size                 | Dynamically modifying collider size                        | ![Demo Image](READMEimages/change_box_collider_size.gif)                         |
| 9. Modify /<br> Collider Type                     | Dynamically modifying collider type                                | ![Demo Image](READMEimages/change_collider_type.gif)                             |
| 9. Modify /<br>  Velocity                  | Modifying velocity                                      | ![Demo Image](READMEimages/change_velocity.gif)                                  |
| 9. Modify /<br>  Kinematic Motion                         | Moving kinematic objects that collide with dynamic objects    | ![Demo Image](READMEimages/kinematic_motion.gif)                                 |
| 9. Modify /<br>  Surface Velocity                  | Dynamically modifying surface velocity                              | ![Demo Image](READMEimages/change_surface_velocity.gif)                          |
| 9. Modify /<br>  Collider Filter                   | Dynamically modifying collider filters                        | ![Demo Image](READMEimages/change_collider_filter.gif)                           |
| 9. Modify /<br>  Scale                             | Dynamically modifying scale                                | ![Demo Image](READMEimages/change_scale.gif)                                     |
| 9. Modify /<br>  Impulse                            | Applying impulses                                 | ![Demo Image](READMEimages/apply_impulse.gif)                                    |
| 9. Modify /<br> Broadphase Pairs                    | Filter out collision by explicitly deleting pairs from the broad phase  |   ![Modify broadphase Sample](READMEimages/modify_broadphase_pairs.gif)            |
| 9. Modify /<br> Contacts                   | Dynamically modifying the results of contact generation to produce special effects |   ![Modify contacts](READMEimages/modify_contact_jacobians.gif)                    |
| 9. Modify /<br> Narrowphase Contacts                | Dynamically add new contacts in narrowphase                        |   ![Modify Narrowphase contacts](READMEimages/modify_narrowphase_contacts.gif)     | 
| 10. Character Controller /<br> Character Controller                      | A rudimentary 3rd-person character controller       | ![Character Control](READMEimages/character_controller.gif)                      |
| 11. Immediate Mode /<br> Pool                                     | A pool game that uses "immediate mode" to project the ball movements before the shot is taken            | ![Immediate physics](READMEimages/pool.gif)                                      |
| 12. Planet Gravity /<br> Planet Gravity                            | Stress test of many asteroids orbiting a planet            | ![Planet Gravity](READMEimages/planet_gravity.gif)                               |
| 13. Raycast Car /<br> Raycast Car                               | Drivable vehicles                   | ![Raycast Car](READMEimages/raycast_car.gif)                                        |
