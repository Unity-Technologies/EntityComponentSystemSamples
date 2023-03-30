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
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new LifeTime { Value = authoring.Value });
    }
}

[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial struct LifeTimeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        using (var commandBuffer = new EntityCommandBuffer(Allocator.TempJob))
        {
            foreach (var(timer, entity) in SystemAPI.Query<RefRW<LifeTime>>().WithEntityAccess())
            {
                timer.ValueRW.Value -= 1;

                if (timer.ValueRW.Value < 0f)
                {
                    commandBuffer.DestroyEntity(entity);
                }
            }

            commandBuffer.Playback(state.EntityManager);
        }
    }
}
