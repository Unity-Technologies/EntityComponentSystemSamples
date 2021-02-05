# Simple BlobAsset Sample (Not scalable)

This sample demonstrates how to convert assets or authoring component data to an BlobAsset in the simplest possible way.

NOTE: This approach is not scalable in terms of conversion performance. Depending on your scalability needs this may or may not be a good fit.
A more complex but scalable conversion approach is shown in Advanced/BlobAssetScalable

## What does it show

Converts an AnimationCurve to a blob asset. The samples uses it to animate the y position of the transform.

It shows how to create a blob asset during converrsion.

It uses `BlobAssetStore` to manage lifetime of the generated blob assets.
It uses `BlobAssetStore.AddUniqueBlobAsset` to ensue that unique blob assets are automatically shared.
