using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;

namespace Samples.Common
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(RandomInitialHeadingSystem))]
    public class RandomInitialHeadingBarrier : BarrierSystem
    { }

    public class RandomInitialHeadingSystem : JobComponentSystem
    {
        private RandomInitialHeadingBarrier m_Barrier;

        protected override void OnCreateManager()
        {
            m_Barrier = World.Active.GetOrCreateManager<RandomInitialHeadingBarrier>();
        }

        struct SetInitialHeadingJob : IJobProcessComponentDataWithEntity<RandomInitialHeading, Heading>
        {
            public EntityCommandBuffer.Concurrent Commands;
            public Unity.Mathematics.Random Random;

            public void Execute(Entity entity, int index, [ReadOnly] ref RandomInitialHeading randomInitialHeading, ref Heading heading)
            {
                heading = new Heading
                {
                    Value = Random.NextFloat3Direction()
                };

                Commands.RemoveComponent<RandomInitialHeading>(index, entity);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new SetInitialHeadingJob
            {
                Commands = m_Barrier.CreateCommandBuffer().ToConcurrent(),
                Random = new Random(0xabcdef)
            };
            var handle = job.Schedule(this, inputDeps);
            m_Barrier.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}
