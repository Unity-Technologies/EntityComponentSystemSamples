using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

public struct LifeTime : IComponentData
{
    public int Value;
}

public class LifeTimeAuthoring : MonoBehaviour
{
    [Tooltip("The number of frames until the entity should be destroyed.")]
    public int Value;
}

public class LifeTimeBaker : Baker<LifeTimeAuthoring>
{
    public override void Bake(LifeTimeAuthoring authoring)
    {
        AddComponent(new LifeTime { Value = authoring.Value });
    }
}

[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial class LifeTimeSystem : SystemBase
{
    protected override void OnUpdate()
    {
        using (var commandBuffer = new EntityCommandBuffer(Allocator.TempJob))
        {
            Entities
                .WithName("DestroyExpiredLifeTime")
                .ForEach((Entity entity, ref LifeTime timer) =>
                {
                    timer.Value -= 1;

                    if (timer.Value < 0f)
                    {
                        commandBuffer.DestroyEntity(entity);
                    }
                }).Run();

            commandBuffer.Playback(EntityManager);
        }
    }
}
