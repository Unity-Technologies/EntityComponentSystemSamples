using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

public struct EntityUpdater : IComponentData
{
    public int TimeToDie;
    public int TimeToMove;
}

public class UpdateBoxRampState : MonoBehaviour
{
    public int TimeToDie;
    public int TimeToMove;

    class UpdateBoxRampStateBaker : Baker<UpdateBoxRampState>
    {
        public override void Bake(UpdateBoxRampState authoring)
        {
            AddComponent(new EntityUpdater { TimeToDie = authoring.TimeToDie, TimeToMove = authoring.TimeToMove });
        }
    }
}

[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial class EntityUpdaterSystem : SystemBase
{
    protected override void OnUpdate()
    {
        using (var commandBuffer = new EntityCommandBuffer(Allocator.TempJob))
        {
#if !ENABLE_TRANSFORM_V1
            foreach (var(updater, localTransform, entity) in SystemAPI.Query<RefRW<EntityUpdater>, RefRW<LocalTransform>>().WithEntityAccess())
#else
            foreach (var(updater, position, entity) in SystemAPI.Query<RefRW<EntityUpdater>, RefRW<Translation>>().WithEntityAccess())
#endif
            {
                if (updater.ValueRW.TimeToDie-- == 0)
                {
                    commandBuffer.DestroyEntity(entity);
                }

                if (updater.ValueRW.TimeToMove-- == 0)
                {
#if !ENABLE_TRANSFORM_V1
                    localTransform.ValueRW.Position += new float3(0, -2, 0);
                    commandBuffer.SetComponent(entity, localTransform.ValueRW);
#else
                    position.ValueRW.Value += new float3(0, -2, 0);
                    commandBuffer.SetComponent(entity, position.ValueRW);
#endif
                }
            }
            commandBuffer.Playback(EntityManager);
        }
    }
}
