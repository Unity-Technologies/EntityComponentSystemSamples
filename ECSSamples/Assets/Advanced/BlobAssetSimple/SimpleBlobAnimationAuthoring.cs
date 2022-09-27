using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class SimpleBlobAnimationAuthoring : MonoBehaviour
{
    public AnimationCurve Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    class Baker : Baker<SimpleBlobAnimationAuthoring>
    {
        public override void Bake(SimpleBlobAnimationAuthoring authoring)
        {
            var blob = SimpleAnimationBlob.CreateBlob(authoring.Curve, Allocator.Persistent);

            // Add the generated blob asset to the blob asset store.
            // if another component generates the exact same blob asset, it will automatically be shared.
            // Ownership of the blob asset is passed to the BlobAssetStore,
            // it will automatically manage the lifetime of the blob asset.
            AddBlobAsset(ref blob, out _);
            AddComponent(new SimpleBlobAnimation { Anim = blob });
        }
    }
}
public struct SimpleBlobAnimation : IComponentData
{
    public BlobAssetReference<SimpleAnimationBlob> Anim;
    public float T;
}

[RequireMatchingQueriesForUpdate]
partial class SimpleBlobAnimationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var dt = SystemAPI.Time.DeltaTime;
#if !ENABLE_TRANSFORM_V1
        Entities.ForEach((ref SimpleBlobAnimation anim, ref LocalToWorldTransform transform) =>
#else
        Entities.ForEach((ref SimpleBlobAnimation anim, ref Translation translation) =>
#endif
        {
            anim.T += dt;
#if !ENABLE_TRANSFORM_V1
            transform.Value.Position.y = anim.Anim.Value.Evaluate(anim.T);
#else
            translation.Value.y = anim.Anim.Value.Evaluate(anim.T);
#endif
        }).Run();
    }
}
