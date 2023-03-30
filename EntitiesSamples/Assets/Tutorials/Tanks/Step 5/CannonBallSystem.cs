using Tutorials.Tanks.Execute;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Tutorials.Tanks.Step5
{
    [BurstCompile]
    partial struct CannonBallSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CannonBall>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var cannonBallJob = new CannonBallJob
            {
                ECB = ecb.AsParallelWriter(),
                DeltaTime = SystemAPI.Time.DeltaTime
            };
            cannonBallJob.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct CannonBallJob : IJobEntity
    {
        // A regular EntityCommandBuffer cannot be safely used directly
        // in a parallel scheduled job, so we need a ParallelWriter.
        public EntityCommandBuffer.ParallelWriter ECB;

        // Time cannot be directly accessed from a job, so DeltaTime has to be passed in as a parameter.
        public float DeltaTime;

        // The ChunkIndexInQuery attribute on an int parameter gives us the "chunk index" of the entity.
        // Each chunk can only be processed by a single thread, so those indices are unique to each thread.
        // They are also fully deterministic, regardless of the amounts of parallel processing happening.
        // So those indices are used as a sorting key when recording commands in the EntityCommandBuffer,
        // this way we ensure that the playback of commands is always deterministic.
        void Execute([ChunkIndexInQuery] int chunkIndex, ref CannonBallAspect cannonBall)
        {
            var gravity = new float3(0.0f, -9.82f, 0.0f);
            var invertY = new float3(1.0f, -1.0f, 1.0f);

            cannonBall.Position += cannonBall.Velocity * DeltaTime;

            // bounce on the ground
            if (cannonBall.Position.y < 0.0f)
            {
                cannonBall.Position *= invertY;
                cannonBall.Velocity *= invertY * 0.8f;
            }

            cannonBall.Velocity += gravity * DeltaTime;

            var speed = math.lengthsq(cannonBall.Velocity);
            if (speed < 0.1f) ECB.DestroyEntity(chunkIndex, cannonBall.Self);
        }
    }
}
