using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Miscellaneous.FixedTimestep
{
    public partial struct MoveProjectilesSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Execute.FixedTimestep>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();

            new MoveJob
            {
                TimeSinceLoad = (float)SystemAPI.Time.ElapsedTime,
                ProjectileSpeed = 5.0f,
                ECBWriter = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct MoveJob : IJobEntity
    {
        public float TimeSinceLoad;
        public float ProjectileSpeed;
        public EntityCommandBuffer.ParallelWriter ECBWriter;

        void Execute(Entity projectileEntity, [ChunkIndexInQuery] int chunkIndex, ref LocalTransform transform,
            in Projectile projectile)
        {
            float aliveTime = TimeSinceLoad - projectile.SpawnTime;
            if (aliveTime > 5.0f)
            {
                ECBWriter.DestroyEntity(chunkIndex, projectileEntity);
            }
            transform.Position.x = projectile.SpawnPos.x + aliveTime * ProjectileSpeed;
        }
    }
}

