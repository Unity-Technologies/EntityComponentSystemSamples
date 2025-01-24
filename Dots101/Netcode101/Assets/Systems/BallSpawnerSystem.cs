using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace KickBall
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct BallSpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntityPrefabs>();
            state.RequireForUpdate<BallConfig>();
            state.RequireForUpdate<NetworkTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var prefabs = SystemAPI.GetSingleton<EntityPrefabs>();
            var ballConfig = SystemAPI.GetSingleton<BallConfig>();
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            
            // avoid repeating spawns
            if (!networkTime.IsFirstTimeFullyPredictingTick)
            {
                return;
            }
            
            foreach (var (input, playerTransform, color) in
                     SystemAPI.Query<RefRO<PlayerInput>, RefRO<LocalTransform>, RefRO<Color>>()
                         .WithAll<Player, Simulate>())
            {
                if (!input.ValueRO.SpawnBall.IsSet)
                {
                    continue;
                }

                var ball = state.EntityManager.Instantiate(prefabs.Ball);
                var ballTransform = playerTransform.ValueRO;
                ballTransform.Position.y += ballConfig.SpawnHeight;
                state.EntityManager.SetComponentData(ball, ballTransform);
                state.EntityManager.SetComponentData(ball, color.ValueRO);
            }
        }
    }
}
