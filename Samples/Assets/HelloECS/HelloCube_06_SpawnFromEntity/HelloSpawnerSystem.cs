using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Samples.HelloCube_06
{
    // JobComponentSystems can run on worker threads.
    // However, creating and removing Entities can only be done on the main thread to prevent race conditions.
    // The system uses an EntityCommandBuffer to defer tasks that can't be done inside the Job.
    public class HelloSpawnerSystem : JobComponentSystem
    {
        // EndSimulationBarrier is used to create a command buffer which will then be played back when that barrier system executes.
        EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

        protected override void OnCreateManager()
        {
            // Cache the EndSimulationBarrier in a field, so we don't have to create it every frame
            m_EntityCommandBufferSystem = World.GetOrCreateManager<EndSimulationEntityCommandBufferSystem>();
        }

        struct SpawnJob : IJobProcessComponentDataWithEntity<HelloSpawner, LocalToWorld>
        {
            public EntityCommandBuffer CommandBuffer;

            public void Execute(Entity entity, int index, [ReadOnly] ref HelloSpawner spawner,
                [ReadOnly] ref LocalToWorld location)
            {
                for (int x = 0; x < spawner.CountX; x++)
                {
                    for (int y = 0; y < spawner.CountY; y++)
                    {
                        var instance = CommandBuffer.Instantiate(spawner.Prefab);

                        // Place the instantiated in a grid with some noise
                        var position = math.transform(location.Value,
                            new float3(x * 1.3F, noise.cnoise(new float2(x, y) * 0.21F) * 2, y * 1.3F));
                        CommandBuffer.SetComponent(instance, new Translation {Value = position});
                    }
                }

                CommandBuffer.DestroyEntity(entity);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            //Instead of performing structural changes directly, a Job can add a command to an EntityCommandBuffer to perform such changes on the main thread after the Job has finished.
            //Command buffers allow you to perform any, potentially costly, calculations on a worker thread, while queuing up the actual insertions and deletions for later.

            // Schedule the job that will add Instantiate commands to the EntityCommandBuffer.
            var job = new SpawnJob
            {
                CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer()
            }.ScheduleSingle(this, inputDeps);


            // SpawnJob runs in parallel with no sync point until the barrier system executes.
            // When the barrier system executes we want to complete the SpawnJob and then play back the commands (Creating the entities and placing them).
            // We need to tell the barrier system which job it needs to complete before it can play back the commands.
            m_EntityCommandBufferSystem.AddJobHandleForProducer(job);

            return job;
        }
    }
}
