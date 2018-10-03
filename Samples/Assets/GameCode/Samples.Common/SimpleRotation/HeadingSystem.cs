using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;

namespace Samples.Common
{
    public class HeadingSystem : JobComponentSystem
    {
#pragma warning disable 649
        struct HeadingsGroup
        {

            public ComponentDataArray<Rotation> rotations;

            [ReadOnly] public ComponentDataArray<Heading> headings;
            public readonly int Length;
        }

        [Inject] private HeadingsGroup m_HeadingsGroup;
#pragma warning restore 649
        
        [BurstCompile]
        struct RotationFromHeading : IJobParallelFor
        {
            public ComponentDataArray<Rotation> rotations;
            [ReadOnly] public ComponentDataArray<Heading> headings;

            public void Execute(int i)
            {
                var heading = headings[i].Value;
                var rotation = quaternion.LookRotationSafe(heading, math.up());
                rotations[i] = new Rotation { Value = rotation };
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var rotationFromHeadingJob = new RotationFromHeading
            {
                rotations = m_HeadingsGroup.rotations,
                headings = m_HeadingsGroup.headings,
            };
            var rotationFromHeadingJobHandle = rotationFromHeadingJob.Schedule(m_HeadingsGroup.Length, 64, inputDeps);

            return rotationFromHeadingJobHandle;
        }
    }
}
