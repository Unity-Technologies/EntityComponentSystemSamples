using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

public struct ChangeScaleSettings : IComponentData
{
    public float Min;
    public float Max;
    public float Target;
}

public class ChangeScaleAuthoring : MonoBehaviour
{
    [Range(0, 10)] public float Min = 0;
    [Range(0, 10)] public float Max = 10;
}

class ChangeScaleBaker : Baker<ChangeScaleAuthoring>
{
    public override void Bake(ChangeScaleAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.ManualOverride);
        AddComponent(entity, new ChangeScaleSettings
        {
            Min = authoring.Min,
            Max = authoring.Max,
            Target = math.lerp(authoring.Min, authoring.Max, 0.5f),
        });
    }
}

#if ENABLE_TRANSFORM_V1
[UpdateInGroup(typeof(PostBakingSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
partial class ChangeScaleBakerPostBakingSystem : SystemBase
{
    EntityQuery bakedEntities;
    protected override void OnCreate()
    {
        bakedEntities = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<ChangeScaleSettings>()
            .WithNone<Scale>()
            .Build(this);
        RequireForUpdate(bakedEntities);
    }

    protected override void OnUpdate()
    {
        EntityManager.AddComponent<Scale>(bakedEntities);
        foreach (var scale in SystemAPI.Query<RefRW<Scale>>().WithAll<ChangeScaleSettings>())
        {
            scale.ValueRW.Value = 1;
        }
    }
}
#endif


[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
[BurstCompile]
public partial struct ChangeScaleSystem : ISystem
{
    [BurstCompile]
    public partial struct ChangeScaleJob : IJobEntity
    {
#if !ENABLE_TRANSFORM_V1
        public void Execute(ref ChangeScaleSettings scaleSettings, ref LocalTransform localTransform)
        {
            float oldScale = localTransform.Scale;
#else
        public void Execute(ref ChangeScaleSettings scaleSettings, ref Scale scale)
        {
            float oldScale = scale.Value;
#endif
            float newScale = 1.0f;

            newScale = math.lerp(oldScale, scaleSettings.Target, 0.05f);

            // If we reach the target, get a new target
            if (math.abs(newScale - scaleSettings.Target) < 0.01f)
            {
                scaleSettings.Target = scaleSettings.Target == scaleSettings.Min ? scaleSettings.Max : scaleSettings.Min;
            }

#if !ENABLE_TRANSFORM_V1
            localTransform.Scale = newScale;
#else
            scale.Value = newScale;
#endif
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new ChangeScaleJob().Schedule(state.Dependency);
    }
}
