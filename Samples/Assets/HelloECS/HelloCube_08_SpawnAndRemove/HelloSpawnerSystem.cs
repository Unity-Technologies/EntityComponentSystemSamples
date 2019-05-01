using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.SceneManagement;

using Random = Unity.Mathematics.Random;

namespace Samples.HelloCube_08
{
    // JobComponentSystems can run on worker threads.
    // However, creating and removing Entities can only be done on the main thread to prevent race conditions.
    // The system uses an EntityCommandBuffer to defer tasks that can't be done inside the Job.
    //
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class HelloSpawnerSystem8 : JobComponentSystem
    {
        // BeginInitializationEntityCommandBufferSystem is used to create a command buffer which will then be played back
        // when that barrier system executes.
        //
        // Though the instantiation command is recorded in the SpawnJob, it's not actually processed (or "played back")
        // until the corresponding EntityCommandBufferSystem is updated. To ensure that the transform system has a chance
        // to run on the newly-spawned entities before they're rendered for the first time, the HelloSpawnerSystem
        // will use the BeginSimulationEntityCommandBufferSystem to play back its commands. This introduces a one-frame lag
        // between recording the commands and instantiating the entities, but in practice this is usually not noticeable.
        //
        BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

        protected override void OnCreate()
        {
            // Enable this system only for HelloCube_08 scene.
            Enabled = SceneManager.GetActiveScene().name.StartsWith("HelloCube_08");

            // Cache the BeginInitializationEntityCommandBufferSystem in a field, so we don't have to create it every frame
            m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        struct SpawnJob : IJobForEachWithEntity<HelloSpawner8, LocalToWorld>
        {
            public EntityCommandBuffer.Concurrent CommandBuffer;

            [BurstCompile]
            public void Execute(Entity entity, int index, [ReadOnly] ref HelloSpawner8 spawner, [ReadOnly] ref LocalToWorld location)
            {
                var random = new Random(1);
                
                for (int x = 0; x < spawner.CountX; x++)
                {
                    for (int y = 0; y < spawner.CountY; y++)
                    {
                        Entity instance = CommandBuffer.Instantiate(index, spawner.Prefab);

                        // Place the instantiated in a grid with some noise
                        var position = math.transform(location.Value, new float3(x * 1.3F, noise.cnoise(new float2(x, y) * 0.21F) * 2, y * 1.3F));
                        CommandBuffer.SetComponent(index, instance, new Translation { Value = position });
                        CommandBuffer.SetComponent(index, instance, new LifeTime { Value = random.NextFloat(10.0F, 100.0F) });
                        CommandBuffer.SetComponent(index, instance, new RotationSpeed { RadiansPerSecond = math.radians(random.NextFloat(25.0F, 90.0F)) });
                    }
                }

                CommandBuffer.DestroyEntity(index, entity);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            // Instead of performing structural changes directly, a Job can add a command to an EntityCommandBuffer to
            // perform such changes on the main thread after the Job has finished. Command buffers allow you to perform
            // any, potentially costly, calculations on a worker thread, while queuing up the actual insertions and
            // deletions for later.

            // Schedule the job that will add Instantiate commands to the EntityCommandBuffer.
            var job = new SpawnJob
            {
                CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
            }.Schedule(this, inputDependencies);

            // SpawnJob runs in parallel with no sync point until the barrier system executes.
            // When the barrier system executes we want to complete the SpawnJob and then play back the commands
            // (Creating the entities and placing them). We need to tell the barrier system which job it needs to
            // complete before it can play back the commands.
            m_EntityCommandBufferSystem.AddJobHandleForProducer(job);

            return job;
        }
    }
}
