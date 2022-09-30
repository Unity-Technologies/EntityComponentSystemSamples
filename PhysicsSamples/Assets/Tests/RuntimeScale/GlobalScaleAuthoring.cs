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

    protected override void OnUpdate()
    {
        GlobalScaleComponent singleton = GetSingleton<GlobalScaleComponent>();

        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);

        Entities
            .WithBurst()
            .ForEach((ref Entity e, ref Translation t, ref PhysicsCollider pc) =>
            {
                if (!HasComponent<Scale>(e))
                {
                    if (HasComponent<PhysicsVelocity>(e))
                    {
                        if (singleton.DynamicUniformScale != 1.0f)
                        {
                            ecb.AddComponent(e, new Scale { Value = singleton.DynamicUniformScale});
                        }
                    }
                    else
                    {
                        if (singleton.StaticUniformScale != 1.0f)
                        {
                            ecb.AddComponent(e, new Scale { Value = singleton.StaticUniformScale});
                        }
                    }
                }
            }).Run();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
