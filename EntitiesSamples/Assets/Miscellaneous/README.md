# Entities miscellaneous samples

*Various small samples demonstrating basic ECS code patterns.*

## AnimateGameObject sample

This sample demonstrates an entity that logically controls an animated GameObject.

## ClosestTarget sample

This sample is similar to the [jobs tutorial](../Tutorials/Jobs/README.md): Seekers (green cubes) and Targets (red cubes) each move slowly in a random direction on a 2D plane. A white debug line is drawn from each Seeker to the nearest Target.

In the subscene, the "Simulation" GameObject has a "SettingsAuthoring" component with a "Spatial Partitioning" value which controls how the Seekers find their Targets:

- `None`: The brute force option. For every Seeker, the `NoPartitioning` loops through every Target to find the Target closest to the Seeker.
- `Simple`: This option uses the same spatial partioning demonstrated in [step 4 of the jobs tutorial](../Tutorials/Jobs/README.md#step-4---solution-with-a-parallel-job-and-a-marter-algorithm).
- `KD Tree`: This option uses a [k-d tree](https://en.wikipedia.org/wiki/K-d_tree) (a tree of points in a *k*-dimensional space) for spatial partitioning.

Spatial partitioning allows each Seeker to find its closest target without having to consider *every* Target. Even the simple partitioning solution scales much better than if we use no partitioning at all, and the k-d tree solution performs even better at very large scales. For example, with 30,000 Seekers and 30,000 Targets on my 8-core CPU, the `TargetingSystem` is about twice as fast using the k-d tree solution compared to the simple solution, and more than a hundred times faster compared to the no partitioning solution.


## CrossQuery sample

Demonstrates how to compare entities from two separate queries. At runtime:

- The `SpawnSystem` creates two sets of boxes: 10 white boxes and 10 black boxes.
- The `MoveSystem` moves the white and black boxes back and forth starting in opposite directions.
- The `CollisionSystem` changes the color of a box when it intersects another: white boxes become pink and black boxes become green.

The `CollisionSystem` performs the intersection tests in one of two ways, toggled by an `#if` in the code:

- The `#if true` solution copies the entity id's and transforms of the boxes to arrays, then it loops over every box (using [`SystemAPI.Query`]()) to compare its position against every other box.
- The `#if false` solution does the work in a job and avoids having to copy the boxes by passing the entity chunks to the job.

## CustomTransforms

A simple custom transform system specialized for 2D instead of 3D.

## FirstPersonController sample

A very simple first-person controller that demonstrates basic input handling and coordination with a GameObject (the camera).

## FixedTimestep sample

This sample demonstrates how to update systems at a fixed rate, similar to the `MonoBehaviour.FixedUpdate` method.

## ProceduralMesh sample

Sets an entity to render a mesh generated at runtime.

## RandomSpawn sample

At runtime:

- The `RandomSpawn` system spawns 200 boxes at regular intervals and positions them at random points along the edge of a circle.
- The `MovementSystem` moves the boxes downward and destroys them when their y coordinate becomes less than zero.

Because the boxes are positioned in a parallel job, randomly positioning the boxes requires a unique random seed value for every box.

## RenderSwap sample

Change the material of rendered entities at runtime.

## ShaderGraph sample

This sample defines material override components, which allow you to control shader parameters by setting the override component values.

## Splines sample

This sample uses baking to define a spline. At runtime, boxes move along the spline paths. 

## StateChange samples

These samples demonstrate different ways of expressing state changes. Numerous cubes are spawned on a plane, and clicking toggles all cubes within a radius between two states: white and stationary; or red and spinning. There are four solutions:

- **Enableable component**: The cube state is represented by enabling and disabling a component.
- **Structural change**: The cube state is represented by adding and removing a component.
- **Value change**: The cube state is represented by a component value.

## TextureUpdate sample

This sample uses Burst-compiled jobs to efficiently generate a texture.

## WireframeBlob sample

This sample constructs a blob wireframe for a complex mesh. Enable Gizmos to see the wireframe. In game, click on the ground to spawn additional instances.
