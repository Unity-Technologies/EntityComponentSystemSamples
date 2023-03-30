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
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new GlobalScaleComponent
            {
                DynamicUniformScale = authoring.DynamicUniformScale,
                StaticUniformScale = authoring.StaticUniformScale
            });
        }
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial struct GlobalScaleSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GlobalScaleComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        GlobalScaleComponent singleton = SystemAPI.GetSingleton<GlobalScaleComponent>();

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
    }
}
