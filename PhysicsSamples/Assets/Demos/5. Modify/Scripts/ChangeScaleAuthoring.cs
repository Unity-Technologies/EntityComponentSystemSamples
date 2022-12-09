using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
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

        AddComponent(entity, new Scale
        {
            Value = 1
        });
    }
}


[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
[BurstCompile]
public partial struct ChangeScaleSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public partial struct ChangeScaleJob : IJobEntity
    {
        public void Execute(ref ChangeScaleSettings scaleSettings, ref Scale scale)
        {
            float newScale = 1.0f;
            float oldScale = scale.Value;

            newScale = math.lerp(oldScale, scaleSettings.Target, 0.05f);

            // If we reach the target, get a new target
            if (math.abs(newScale - scaleSettings.Target) < 0.01f)
            {
                scaleSettings.Target = scaleSettings.Target == scaleSettings.Min ? scaleSettings.Max : scaleSettings.Min;
            }

            scale.Value = newScale;
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new ChangeScaleJob().Schedule(state.Dependency);
    }
}
