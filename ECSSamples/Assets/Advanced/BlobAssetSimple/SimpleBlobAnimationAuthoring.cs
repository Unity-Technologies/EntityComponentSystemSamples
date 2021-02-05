using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class SimpleBlobAnimationAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public AnimationCurve Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var blob = SimpleAnimationBlob.CreateBlob(Curve, Allocator.Persistent);

        // Add the generated blob asset to the blob asset store.
        // if another component generates the exact same blob asset, it will automatically be shared.
        // Ownership of the blob asset is passed to the BlobAssetStore,
        // it will automatically manage the lifetime of the blob asset.
        conversionSystem.BlobAssetStore.AddUniqueBlobAsset(ref blob);

        dstManager.AddComponentData(entity, new SimpleBlobAnimation { Anim = blob });
    }
}
public struct SimpleBlobAnimation : IComponentData
{
    public BlobAssetReference<SimpleAnimationBlob> Anim;
    public float T;
}

partial class SimpleBlobAnimationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;
        Entities.ForEach((ref SimpleBlobAnimation anim, ref Translation translation) =>
        {
            anim.T += dt;
            translation.Value.y = anim.Anim.Value.Evaluate(anim.T);
        }).Run();
    }
}
