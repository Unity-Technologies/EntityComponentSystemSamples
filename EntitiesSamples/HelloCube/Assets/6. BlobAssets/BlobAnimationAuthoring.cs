using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace HelloCube.BlobAssets
{
    /// <summary>
    /// This component holds the BlobAssetReference to the BlobData so it can be accessed in a System
    /// </summary>
    public struct Animation : IComponentData
    {
        public BlobAssetReference<AnimationBlobData> AnimBlobReference;
        public float T;
    }

    /// <summary>
    /// Very simple animation curve blob data that uses linear interpolation at fixed intervals.
    /// Blob data is constructed from a UnityEngine.AnimationCurve
    /// </summary>
    public struct AnimationBlobData
    {
        public BlobArray<float> Keys;
        public float InvLength;
        public float KeyCount;

    }

    public class BlobAnimationAuthoring : MonoBehaviour
    {
        public AnimationCurve Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        class Baker : Baker<BlobAnimationAuthoring>
        {
            public override void Bake(BlobAnimationAuthoring authoring)
            {
                // Create the BlobAssetReference that facilitates access to the BlobAsset
                var blobReference = CreateBlob(authoring.Curve, Allocator.Persistent);

                // Add the generated BlobAsset to the Baker (and beyond that to the BlobAssetStore) by passing the BlobAssetReference
                // Ownership of the BlobAsset is passed to the BlobAssetStore,
                // it will automatically manage the lifetime and deduplication of the BlobAsset.
                AddBlobAsset<AnimationBlobData>(ref blobReference, out _);
                AddComponent(new Animation {AnimBlobReference = blobReference});
            }

            BlobAssetReference<AnimationBlobData> CreateBlob(AnimationCurve curve, Allocator allocator,
                Allocator allocatorForTemp = Allocator.TempJob)
            {
                using (var blobBuilder = new BlobBuilder(allocatorForTemp))
                {
                    // Use the BlobBuilder to construct the BlobAsset
                    ref var root = ref blobBuilder.ConstructRoot<AnimationBlobData>();
                    int keyCount = 12;

                    float endTime = curve[curve.length - 1].time;
                    root.InvLength = 1.0F / endTime;
                    root.KeyCount = keyCount;

                    // Allocate space for the BlobArray, then fill it up
                    var array = blobBuilder.Allocate(ref root.Keys, keyCount + 1);
                    for (int i = 0; i < keyCount; i++)
                    {
                        float t = (float) i / (float) (keyCount - 1) * endTime;
                        array[i] = curve.Evaluate(t);
                    }

                    array[keyCount] = array[keyCount - 1];

                    // Generate the BlobAssetReference from the BlobAsset
                    return blobBuilder.CreateBlobAssetReference<AnimationBlobData>(allocator);
                }
            }
        }
    }
}
