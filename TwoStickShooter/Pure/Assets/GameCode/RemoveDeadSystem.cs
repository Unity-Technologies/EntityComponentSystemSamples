using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

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
        struct Data
        {
            [ReadOnly] public EntityArray Entity;
            [ReadOnly] public ComponentDataArray<Health> Health;
        }

        struct PlayerCheck
        {
            [ReadOnly] public ComponentDataArray<PlayerInput> PlayerInput;
        }

        [Inject] private Data m_Data;
        [Inject] private PlayerCheck m_PlayerCheck;
        [Inject] private RemoveDeadBarrier m_RemoveDeadBarrier;

        [ComputeJobOptimization]
        struct RemoveReadJob : IJob
        {
            public bool playerDead;
            [ReadOnly] public EntityArray Entity;
            [ReadOnly] public ComponentDataArray<Health> Health;
            public EntityCommandBuffer Commands;

            public void Execute()
            {
                for (int i = 0; i < Entity.Length; ++i)
                {
                    if (Health[i].Value <= 0.0f || playerDead)
                    {
                        Commands.DestroyEntity(Entity[i]);
                    }
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new RemoveReadJob
            {
                playerDead = m_PlayerCheck.PlayerInput.Length == 0,
                Entity = m_Data.Entity,
                Health = m_Data.Health,
                Commands = m_RemoveDeadBarrier.CreateCommandBuffer(),
            }.Schedule(inputDeps);
        }
    }

}
