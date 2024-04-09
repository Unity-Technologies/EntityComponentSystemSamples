using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Baking.BlobAssetBaker
{
    public class BlobAnimationAuthoring : MonoBehaviour
    {
        // We'll bake this animation curve into a blob.
        public AnimationCurve Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        class Baker : Baker<BlobAnimationAuthoring>
        {
            public override void Bake(BlobAnimationAuthoring authoring)
            {
                // TransformUsageFlags.Dynamic gives the entity LocalTransform and LocalToWorld components.
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                var blobReference = CreateBlob(authoring.Curve, Allocator.Persistent);

                // Ownership of the BlobAsset is passed to the BlobAssetStore,
                // which will automatically manage the lifetime and deduplication of the BlobAsset.
                AddBlobAsset<AnimationBlobData>(ref blobReference, out _);

                AddComponent(entity, new Animation { AnimBlobReference = blobReference });
            }

            BlobAssetReference<AnimationBlobData> CreateBlob(AnimationCurve curve, Allocator allocator,
                Allocator builderAllocator = Allocator.TempJob)
            {
                // Make sure to dispose the builder once the blob asset is created.
                using (var blobBuilder = new BlobBuilder(builderAllocator))
                {
                    // A blob asset is built starting with a root struct (AnimationBlobData in this case).
                    ref var root = ref blobBuilder.ConstructRoot<AnimationBlobData>();
                    int keyCount = 12;

                    float endTime = curve[curve.length - 1].time;
                    root.InvLength = 1.0F / endTime;
                    root.KeyCount = keyCount;

                    // Build the Keys array.
                    var array = blobBuilder.Allocate(ref root.Keys, keyCount + 1);
                    for (int i = 0; i < keyCount; i++)
                    {
                        float time = (float)i / (float)(keyCount - 1) * endTime;
                        array[i] = curve.Evaluate(time);
                    }

                    array[keyCount] = array[keyCount - 1];

                    // Copy the builder data into the final blob asset form.
                    return blobBuilder.CreateBlobAssetReference<AnimationBlobData>(allocator);
                }
            }
        }
    }

    public struct Animation : IComponentData
    {
        public BlobAssetReference<AnimationBlobData> AnimBlobReference;
        public float Time;
    }

    // The root struct of our blob.
    // A very simple animation curve using linear interpolation at fixed intervals.
    public struct AnimationBlobData
    {
        public BlobArray<float> Keys;
        public float InvLength;
        public float KeyCount;
    }
}
