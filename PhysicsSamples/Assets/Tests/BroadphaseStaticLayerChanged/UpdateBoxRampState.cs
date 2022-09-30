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
            Entities.ForEach(
                (Entity entity, ref EntityUpdater updater, ref Translation position) =>
                {
                    if (updater.TimeToDie-- == 0)
                    {
                        commandBuffer.DestroyEntity(entity);
                    }

                    if (updater.TimeToMove-- == 0)
                    {
                        position.Value += new float3(0, -2, 0);
                        commandBuffer.SetComponent(entity, position);
                    }
                }
                ).Run();

            commandBuffer.Playback(EntityManager);
        }
    }
}
