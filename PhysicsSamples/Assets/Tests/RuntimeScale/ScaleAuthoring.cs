using UnityEngine;
using Unity.Entities;
using Unity.Physics.Authoring;
using Unity.Transforms;

[TemporaryBakingType]
struct BakedUniformScaleComponent : IComponentData
{
    public float UniformScale;
}

public class ScaleAuthoring : MonoBehaviour
{
    public float UniformScale = 1.0f;

    class ScaleBaker : Baker<ScaleAuthoring>
    {
        public override void Bake(ScaleAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new BakedUniformScaleComponent { UniformScale = authoring.UniformScale });
        }
    }
}

[RequireMatchingQueriesForUpdate]
[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
[UpdateAfter(typeof(RigidbodyBakingSystem))]
partial struct UniformScaleBakingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var(transform, authoring) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<BakedUniformScaleComponent>>())
        {
            transform.ValueRW.Scale = authoring.ValueRO.UniformScale;
        }
    }
}
