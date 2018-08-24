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
        struct HeadingsGroup
        {
            public ComponentDataArray<Rotation> rotations;
            [ReadOnly] public ComponentDataArray<Heading> headings;
            public readonly int Length;
        }

        [Inject] private HeadingsGroup m_HeadingsGroup;

        [BurstCompile]
        struct RotationFromHeading : IJobParallelFor
        {
            public ComponentDataArray<Rotation> rotations;
            [ReadOnly] public ComponentDataArray<Heading> headings;

            public void Execute(int i)
            {
                var heading = headings[i].Value;
                var rotation = quaternion.lookRotation(heading, math.up());
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
