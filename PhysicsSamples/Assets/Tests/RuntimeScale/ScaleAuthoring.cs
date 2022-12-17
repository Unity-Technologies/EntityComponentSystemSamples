using UnityEngine;
using Unity.Entities;
using Unity.Physics.Authoring;
using Unity.Transforms;

#if !ENABLE_TRANSFORM_V1
[TemporaryBakingType]
struct BakedUniformScaleComponent : IComponentData
{
    public float UniformScale;
}
#endif

public class ScaleAuthoring : MonoBehaviour
{
    public float UniformScale = 1.0f;

    class ScaleBaker : Baker<ScaleAuthoring>
    {
        public override void Bake(ScaleAuthoring authoring)
        {
#if !ENABLE_TRANSFORM_V1
            AddComponent(new BakedUniformScaleComponent { UniformScale = authoring.UniformScale });
#else
            AddComponent(new Scale() { Value = authoring.UniformScale });
#endif
        }
    }
}

#if !ENABLE_TRANSFORM_V1
[RequireMatchingQueriesForUpdate]
[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
[UpdateAfter(typeof(PostProcessPhysicsTransformBakingSystem))]
partial class UniformScaleBakingSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (transform, authoring) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<BakedUniformScaleComponent>>())
        {
            transform.ValueRW.Scale = authoring.ValueRO.UniformScale;
        }
    }
}
#endif
