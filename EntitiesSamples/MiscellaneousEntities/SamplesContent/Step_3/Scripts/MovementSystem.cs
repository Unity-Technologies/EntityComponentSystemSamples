using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace RandomSpawn
{
    [BurstCompile]
    [WithAll(typeof(Cube))]
    partial struct FallingCubeJob : IJobEntity
    {
        public float3 Movement;
        public EntityCommandBuffer.ParallelWriter ECB;

        void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, TransformAspect cubeTransform)
        {
            cubeTransform.TranslateLocal(Movement);
            if (cubeTransform.LocalPosition.y < 0)
            {
                ECB.DestroyEntity(chunkIndex, entity);
            }
        }
    }

    [BurstCompile]
    public partial struct MovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state) { }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var job = new FallingCubeJob
            {
                Movement = new float3(0, SystemAPI.Time.DeltaTime * -20, 0),
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            };
            job.ScheduleParallelByRef();
        }
    }
}
