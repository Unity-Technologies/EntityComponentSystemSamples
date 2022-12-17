# MiscellaneousEntities samples

Various small samples demonstrating basic ECS code patterns.

| &#x1F4DD; NOTE |
| :- |
| Load each sample using the `Samples` window (which is under the `Samples` menu in the main menubar). |

<br>

## CrossQuery sample

At runtime, the `SpawnSystem` creates two sets of boxes: 10 white boxes and 10 black boxes. The `MoveSystem` moves the white and black boxes back and forth starting in opposite directions. The `CollisionSystem` changes the color of a box when it intersects another: white boxes become pink and black boxes become green.

The `CollisionSystem` performs the intersection tests in one of two ways, toggled by a `#if` in the code:

- The `#if true` solution copies the entity id's and transforms of the boxes to arrays, then it loops over every box (using [`SystemAPI.Query`]()) to compare its position against every other box.
- The `#if false` solution does the work in a job and avoids having to copy the boxes by passing the box entity chunks to the job.

<br>

## RandomSpawn sample

At runtime, the `RandomSpawn` system spawns 200 boxes at regular intervals and positions them at random points along the edge of a circle. The `MovementSystem` moves the boxes downward and destroys them when their y coordinate becomes less than zero.

Because the boxes are positioned in a parallel job, randomly positioning the boxes requies a unique random seed value for every box.

<br>

## ClosestTarget sample

This sample is similar to the [jobs tutorial](../JobsTutorial/README.md): Seekers (green cubes) and Targets (red cubes) each move slowly in a random direction on a 2D plane. A white debug line is drawn from each Seeker to the nearest Target.

In the Sub Scene, the "Simulation" GameObject has a "SettingsAuthoring" component with a "Spatial Partitioning" value which controls how the Seekers find their Targets:

- `None`: With this brute force option, the `NoPartitioning` job loops through every Seeker, and for every Seeker loops through every Target to find the Target closest to the Seeker.
- `Simple`: This option uses the same spatial partioning demonstrated in [step 4 of the jobs tutorial](../jobs_tutorial/README.md#step-4---solution-with-a-parallel-job-and-sorting-the-targets).
- `KD Tree`: This option uses a [k-d tree](https://en.wikipedia.org/wiki/K-d_tree) (a tree of points in a *k*-dimensional space) for spatial partitioning. 

Spatial partitioning allows each Seeker to find its closest target without having to consider *every* Target. Even the simple partitioning solution scales much better than if we use no partitioning at all, and the k-d tree solution performs even better at very large scales. For example, with 30,000 Seekers and 30,000 Targets on my 8-core machine, the `TargetingSystem` is about twice as fast using the k-d tree solution compared to the simple solution and more than a hundred times faster compared to the no partitioning solution.

<br>

## StateChange samples

The StateChange samples demonstrate different ways of expressing state changes. Numerous cubes are spawned on a plane, and clicking toggles all cubes within a radius between two states: white and stationary; or red and spinning. There are four variants:

- **Value change**: The cube state is represented by a component value. 
- **Structural change**: The cube state is represented by adding and removing a component.
- **Enableable component**: The cube state is represented by enabling and disabling a component.
- **Profiling**: This variant is just for comparing performance of the other three variants. (For easier profiling, the cube state is set every frame rather than only when the user clicks.)

<br>

## FixedTimestep sample

The FixedTimestep sample demonstrates how to update systems at a fixed rate, similar to the `MonoBehaviour.FixedUpdate` method.

<br>

## BakingDependencies sample

The BakingDependencies sample demonstrates how a baker and a baking system can react to changes made on the authoring data.

The ImageGeneratorAuthoring component references an image and a ScriptableObject asset, which contains a float value, a mesh, and a material. During baking, this component generates one primitive per pixel in the image and sets the color correspondingly.

Modifying any of the authoring component's parameters will trigger a re-bake of the necessary entities. For example, modifying the float value will re-bake both GameObjects (because they both use the asset), but modifying the "hello.png" image will only re-bake the one GameObject which depends upon it.

<br>

## BlobAssetBaking sample

The BlobAssetBaking sample demonstrates how to bake BlobAssets in an efficient and scalable way using baking systems:

* How to create and revert BlobAssets in a BakingSystem
* How to avoid generating blobs that were already generated
* How to efficiently extract all inputs for blob generation from authoring data and then perform all blob generation in a bursted and parallel job

The Sub Scene contains 256 GameObjects, split in four types: Capsules, Cubes, Cylinders and Spheres. 
Each GameObject has a `MeshBBAuthoring` component that defines the information we want to store in a blob asset. 
(The GameObjects are stored as a nested prefab to save disk space).

The `MeshBBAuthoring` baker collects the mesh vertices and additional information (a.o. mesh.GetHashCode) and stores them in a `BakingType` buffer and component respectively.

The `ComputeBlobAssetSystem` BakingSystem is set up in three main steps:

1. In the first step, all to-be-created BlobAssets are filtered. Only BlobAssets that are not already present in the BlobAssetStore and are not already being processed this run, are added to a list for processing. 
2. In the second step, all these unique BlobAssets are created and their `BlobAssetReference`s are stored matching their Hashes. 
3. In the third step, all entities get the correct `BlobAssetReference`.

After baking, the `MeshBBRenderSystem` uses the blob asset to draw a bounding box for each of the 256 baked entities. (The renderer uses the Unity `Debug.Draw()` function, so the bounding box only appears in the Scene view.)

Understand that, when creating BlobAssets in a Baker, the Baker tracks and reverts the BlobAssets when it is rerun, and updates the refcounts in the BlobAssetStore to match changes. However, when creating BlobAssets in a baking system, all of this is *not* done automatically, so we manually check if the entities have a different (or no) BlobAsset compared to last frame, and if so, update the BlobAssetStore to match. If an Entity is removed altogether, we also have to cleanup the BlobAssets (using an `ICleanupComponent`).

The prefabs:

* **Capsule x4** — a square made up of four Capsules.
* **Capsule x16** — a square made up of four Capsule x4 squares.
* **Capsule x64** — a cube made up of four Capsule x16 squares.
* **Cube x4** — a square made up of four Cubes.
* **Cube x16** — a square made up of four Cube x4 squares.
* **Cube x64** — a cube made up of four Cube x16 squares.
* **Cylinder x4** — a square made up of four Cylinders.
* **Cylinder x16** — a square made up of four Cylinder x4 squares.
* **Cylinder x64** — a cube made up of four Cylinder x16 squares.
* **Sphere x4** — a square made up of four Spheres.
* **Sphere x16** — a square made up of four Sphere x4 squares.
* **Sphere x64** — a cube made up of four Sphere x16 squares.

The scripts:

* **MeshBBAuthoring** — defines the mesh and scale from which to compute the bounding boxes. Although each component in the sample starts out  the same, you can assign different values to each component.
* **MeshBBComponent** — defines a.o. the structure of the blob asset used to store the computed bounding boxes and also the IComponentData struct used to assign a BlobAssetReference to an individual entity.
* **ComputeBlobAssetSystem** — Filters, creates and reverts each unique combination of the values of Mesh and Scale encountered in the `MeshBBAuthoring` components to a unique blob asset.
* **MeshRenderSystem** — draws the bounding boxes in the Unity Scene view.

<br>


