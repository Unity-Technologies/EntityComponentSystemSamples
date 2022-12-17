using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

public struct GlobalScaleComponent : IComponentData
{
    public float DynamicUniformScale;
    public float StaticUniformScale;
}

public class GlobalScaleAuthoring : MonoBehaviour
{
    public float DynamicUniformScale = 1.0f;
    public float StaticUniformScale = 1.0f;

    class GlobalScaleBaker : Baker<GlobalScaleAuthoring>
    {
        public override void Bake(GlobalScaleAuthoring authoring)
        {
            AddComponent(new GlobalScaleComponent
            {
                DynamicUniformScale = authoring.DynamicUniformScale,
                StaticUniformScale = authoring.StaticUniformScale
            });
        }
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial class GlobalScaleSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<GlobalScaleComponent>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        GlobalScaleComponent singleton = SystemAPI.GetSingleton<GlobalScaleComponent>();

#if !ENABLE_TRANSFORM_V1
        foreach (var(localPosition, collider, entity) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
        {
            if (SystemAPI.HasComponent<PhysicsVelocity>(entity))
            {
                if (singleton.DynamicUniformScale != 1.0f)
                {
                    localPosition.ValueRW.Scale = singleton.DynamicUniformScale;
                }
            }
            else
            {
                if (singleton.StaticUniformScale != 1.0f)
                {
                    localPosition.ValueRW.Scale = singleton.StaticUniformScale;
                }
            }
        }

#else
        foreach (var(scale, collider, entity) in SystemAPI.Query<RefRW<Scale>, RefRW<PhysicsCollider>>().WithEntityAccess())
        {
            if (SystemAPI.HasComponent<PhysicsVelocity>(entity))
            {
                if (singleton.DynamicUniformScale != 1.0f)
                {
                    scale.ValueRW.Value = singleton.DynamicUniformScale;
                }
            }
            else
            {
                if (singleton.StaticUniformScale != 1.0f)
                {
                    scale.ValueRW.Value = singleton.StaticUniformScale;
                }
            }
        }
#endif
    }
}
