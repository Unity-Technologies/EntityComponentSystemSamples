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
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EntityUpdater { TimeToDie = authoring.TimeToDie, TimeToMove = authoring.TimeToMove });
        }
    }
}

[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial struct EntityUpdaterSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        using (var commandBuffer = new EntityCommandBuffer(Allocator.Temp))
        {
            foreach (var(updater, localTransform, entity) in SystemAPI.Query<RefRW<EntityUpdater>, RefRW<LocalTransform>>().WithEntityAccess())
            {
                if (updater.ValueRW.TimeToDie-- == 0)
                {
                    commandBuffer.DestroyEntity(entity);
                }

                if (updater.ValueRW.TimeToMove-- == 0)
                {
                    localTransform.ValueRW.Position += new float3(0, -2, 0);
                    commandBuffer.SetComponent(entity, localTransform.ValueRW);
                }
            }
            commandBuffer.Playback(state.EntityManager);
        }
    }
}
