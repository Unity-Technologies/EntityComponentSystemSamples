# Entities baking samples

## AutoAuthoring

A solution for conveniently creating authoring components that simply copy each field of the authoring MonoBehaviour to a corresponding field of an IComponentData.

## BakingDependencies sample

This sample demonstrates how a baker and a baking system can react to changes made on the authoring data.

The `ImageGeneratorAuthoring` component references an image and a `ScriptableObject` asset, which contains a float value, a mesh, and a material. During baking, this component generates one primitive per pixel in the image and sets the color correspondingly.

Modifying any of the authoring component's fields will trigger a re-bake of the necessary GameObjects in the subscene. For example, modifying the float field will re-bake both GameObjects (because they both use the asset), but modifying the "hello.png" image will only re-bake the one GameObject which depends upon it.

## BakingTypes sample

This sample doesn't do anything at runtime, but it uses baking to create a bounding box around each set of cubes (enable Gizmos to see the white debug lines). When you drag the cubes around in the Scene window, you'll see the bounding box update as you drag because baking is re-triggered as you edit the subscene.

## BlobAssetBaker sample

This sample creates a [BlobAsset](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/blob-assets-concept.html) during Baking. At runtime, the animation curve stored in the BlobAsset is used to animate the y position of a cube.

## BlobAssetBakingSystem sample

This sample demonstrates how to bake BlobAssets in an efficient and scalable way using baking systems. In the code:

- The subscene contains 256 GameObjects, split in four types: Capsules, Cubes, Cylinders and Spheres.
- Each GameObject in the subscene has a `MeshBBAuthoring` component that defines the information we want to store in a blob asset.
- The `MeshBBAuthoring` baker stores the mesh vertices in a `BakingType` buffer and additional information in a `BakingType` component.
- The `MeshBBRenderSystem`, which updates in edit mode, uses the blob asset to draw a debug bounding box around the 256 baked entities.

The `ComputeBlobAssetSystem` BakingSystem is set up in three main steps:

1. BlobAssets that are not already present in the BlobAssetStore are added to a list for processing.
2. Unique BlobAssets are identified by their hashes.
3. `BlobAssetReference` are stored in entity components.

Note that bakers track which BlobAssets are referenced by which entities and keep the BlobAssetStore updated accordingly. However, BlobAssets created in a *baking system* are not tracked automatically, so the baking system must manually check if the entities reference different BlobAssets compared to last bake and update the BlobAssetStore manually. If an Entity is removed altogether, the baking systems must clean up any baked BlobAssets that the entity might reference.

## PrefabReference sample

This sample demonstrates how to use an `EntityPrefabReference`. Whereas directly baking a prefab in each SubScene creates one baked entity per SubScene, an `EntityPrefabReference` allows multiple SubScenes to reference a single baked prefab entity.