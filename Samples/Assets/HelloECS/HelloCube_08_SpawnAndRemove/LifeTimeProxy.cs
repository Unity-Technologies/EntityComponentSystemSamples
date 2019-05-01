using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;

using Random = Unity.Mathematics.Random;

namespace Samples.HelloCube_08
{
    public struct LifeTime : IComponentData
    {
        public float Value;
    }

    // This system updates all entities in the scene with both a RotationSpeed and Rotation component.
    public class LifeTimeSystem : JobComponentSystem
    {
        EntityCommandBufferSystem barrier;
        NativeQueue<float3> queue;
        Entity prefab;
        Random random;

        protected override void OnCreate()
        {
            // Enable this system only for HelloCube_08 scene.
            Enabled = SceneManager.GetActiveScene().name.StartsWith("HelloCube_08");

            barrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            queue = new NativeQueue<float3>(Allocator.Persistent);

            // Cache prefab to entity conversion to not pay cost of conversion every time entity is instantiated.
            GameObject gameObject = (GameObject)Resources.Load("RotatingCube", typeof(GameObject));
            prefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(gameObject, World.Active);

            random = new Random(1);
        }

        protected override void OnDestroy()
        {
            // Native* container must be disposed, otherwise there would be memory leak.
            queue.Dispose();

            base.OnDestroy();
        }

        // Use the [BurstCompile] attribute to compile a job with Burst.
        // You may see significant speed ups, so try it!
        [BurstCompile]
        struct LifeTimeJob : IJobForEachWithEntity<Translation, LifeTime>
        {
            public float DeltaTime;

            [WriteOnly]
            public EntityCommandBuffer.Concurrent CommandBuffer;

            [WriteOnly]
            public NativeQueue<float3>.Concurrent Queue;

            public void Execute(Entity entity, int jobIndex, ref Translation translation, ref LifeTime lifeTime)
            {
                lifeTime.Value -= DeltaTime;

                if (lifeTime.Value < 0.0f)
                {
                    Queue.Enqueue(translation.Value);
                    CommandBuffer.DestroyEntity(jobIndex, entity);
                }
            }
        }

        // OnUpdate runs on the main thread.
        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            var commandBuffer = barrier.CreateCommandBuffer().ToConcurrent();

            var job = new LifeTimeJob()
            {
                DeltaTime = Time.deltaTime,
                CommandBuffer = commandBuffer,
                Queue = queue.ToConcurrent(),

            }.Schedule(this, inputDependencies);

            barrier.AddJobHandleForProducer(job);

            // Can't read and write into queue at the same time.
            job.Complete();

            float3 translation;
            if (queue.TryDequeue(out translation))
            {
                var em = World.Active.EntityManager;

                Entity instance = em.Instantiate(prefab);
                em.SetComponentData(instance, new Translation { Value = translation });
                em.SetComponentData(instance, new LifeTime { Value = random.NextFloat(10.0f, 500.0f) });
                em.SetComponentData(instance, new RotationSpeed { RadiansPerSecond = math.radians(random.NextFloat(-35.0F, -15.0F)) });
            }

            return job;
        }
    }
}
