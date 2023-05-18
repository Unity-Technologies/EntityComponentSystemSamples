using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Miscellaneous.RandomSpawn
{
    public partial struct MovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Execute.RandomSpawn>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();

            new FallingCubeJob
            {
                Movement = new float3(0, SystemAPI.Time.DeltaTime * -20, 0),
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel();
        }
    }

    [WithAll(typeof(Cube))]
    [BurstCompile]
    public partial struct FallingCubeJob : IJobEntity
    {
        public float3 Movement;
        public EntityCommandBuffer.ParallelWriter ECB;

        void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref LocalTransform cubeTransform)
        {
            cubeTransform.Position += Movement;
            if (cubeTransform.Position.y < 0)
            {
                ECB.DestroyEntity(chunkIndex, entity);
            }
        }
    }
}
