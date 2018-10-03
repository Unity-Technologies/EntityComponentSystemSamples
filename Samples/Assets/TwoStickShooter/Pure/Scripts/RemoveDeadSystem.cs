using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;

namespace TwoStickPureExample
{
    public class RemoveDeadBarrier : BarrierSystem
    {
    }

    /// <summary>
    /// This system deletes entities that have a Health component with a value less than or equal to zero.
    /// </summary>
    public class RemoveDeadSystem : JobComponentSystem
    {
#pragma warning disable 649
        struct PlayerCheck
        {
            [ReadOnly] public ComponentDataArray<PlayerInput> PlayerInput;
        }

        [Inject] private PlayerCheck m_PlayerCheck;
        [Inject] private RemoveDeadBarrier m_RemoveDeadBarrier;
#pragma warning restore 649

        [BurstCompile]
        struct RemoveReadJob : IJobProcessComponentDataWithEntity<Health>
        {
            public bool playerDead;
            public EntityCommandBuffer Commands;

            public void Execute(Entity entity, int index, ref Health health)
            {
                if (health.Value <= 0.0f || playerDead)
                    Commands.DestroyEntity(entity);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new RemoveReadJob
            {
                playerDead = m_PlayerCheck.PlayerInput.Length == 0,
                Commands = m_RemoveDeadBarrier.CreateCommandBuffer(),
            }.ScheduleSingle(this, inputDeps);
        }
    }

}
