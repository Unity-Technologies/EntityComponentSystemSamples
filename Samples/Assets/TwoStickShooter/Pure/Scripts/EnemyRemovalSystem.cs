using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace TwoStickPureExample
{
    // Removes enemies that are off screen
    class EnemyRemovalSystem : JobComponentSystem
    {
        public struct BoundaryKillJob : IJobProcessComponentData<Health, Position, Enemy>
        {
            public float MinZ;
            public float MaxZ;

            public void Execute(ref Health health, [ReadOnly] ref Position pos, [ReadOnly] ref Enemy enemyTag)
            {
                if (pos.Value.z > MaxZ || pos.Value.z < MinZ)
                {
                    health.Value = -1.0f;
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (TwoStickBootstrap.Settings == null)
                return inputDeps;

            var boundaryKillJob = new BoundaryKillJob
            {
                MinZ = TwoStickBootstrap.Settings.playfield.yMin,
                MaxZ = TwoStickBootstrap.Settings.playfield.yMax,
            };

            return boundaryKillJob.Schedule(this, inputDeps);
        }
    }
}
