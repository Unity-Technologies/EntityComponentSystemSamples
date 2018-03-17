using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms2D;

namespace TwoStickPureExample
{
    // Removes enemies that are off screen
    class EnemyRemovalSystem : JobComponentSystem
    {
        public struct BoundaryKillJob : IJobProcessComponentData<Health, Position2D, Enemy>
        {
            public float MinY;
            public float MaxY;

            public void Execute(ref Health health, [ReadOnly] ref Position2D pos, [ReadOnly] ref Enemy enemyTag)
            {
                if (pos.Value.y > MaxY || pos.Value.y < MinY)
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
                MinY = TwoStickBootstrap.Settings.playfield.yMin,
                MaxY = TwoStickBootstrap.Settings.playfield.yMax,
            };

            return boundaryKillJob.Schedule(this, 64, inputDeps);
        }
    }
}
