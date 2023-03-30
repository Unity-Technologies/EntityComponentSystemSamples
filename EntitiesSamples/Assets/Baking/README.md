# Baking samples

## Baking dependencies sample

This sample demonstrates how a baker and a baking system can react to changes made on the authoring data.

The `ImageGeneratorAuthoring` component references an image and a `ScriptableObject` asset, which contains a float value, a mesh, and a material. During baking, this component generates one primitive per pixel in the image and sets the color correspondingly.

Modifying any of the authoring component's fields will trigger a re-bake of the necessary GameObjects in the subscene. For example, modifying the float field will re-bake both GameObjects (because they both use the asset), but modifying the "hello.png" image will only re-bake the one GameObject which depends upon it.

<br>

## Baking types sample

This sample doesn't do anything at runtime, but it uses baking to create a bounding box around each set of cubes (enable Gizmos to see the white debug lines). When you drag the cubes around in the Scene window, you'll see the bounding box update as you drag because baking is re-triggered as you edit the subscene.

<br>

## Blob asset baker sample

This sample creates a [BlobAsset](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/blob-assets-concept.html) during Baking. At runtime, the animation curve stored in the BlobAsset is used to animate the y position of a cube.

<br>

## BlobAsset baking sample

This sample demonstrates how to bake BlobAssets in an efficient and scalable way using baking systems:

* How to create BlobAssets in a BakingSystem
* How to avoid generating blobs that were already generated
* How to efficiently extract all inputs for blob generation from authoring data and then perform all blob generation in a Burst-compiled parallel job

In the code:

- The subscene contains 256 GameObjects, split in four types: Capsules, Cubes, Cylinders and Spheres.
- Each GameObject in the subscene has a `MeshBBAuthoring` component that defines the information we want to store in a blob asset.
- The `MeshBBAuthoring` baker stores the mesh vertices in a `BakingType` buffer and additional information in a `BakingType` component.

The `ComputeBlobAssetSystem` BakingSystem is set up in three main steps:

1. All to-be-created BlobAssets are filtered. Only BlobAssets that are not already present in the BlobAssetStore and are not already being processed this run, are added to a list for processing.
2. All these unique BlobAssets are created and their `BlobAssetReference`s are stored matching their Hashes.
3. All entities get the correct `BlobAssetReference`.

After baking, the `MeshBBRenderSystem` uses the blob asset to draw a debug bounding box around the 256 baked entities.

When BlobAssets are created in a baker, the baker tracks which entities reference the BlobAssets, and updates the BlobAssetStore accordingly when the baker re-runs. However, BlobAssets created in a *baking system* are not tracked automatically, so the baking system must manually check if the entities have a different (or no) BlobAsset compared to last frame and update the BlobAssetStore ourselves. If an Entity is removed altogether, we must clean up any baked BlobAssets it might have (using an `ICleanupComponent`).