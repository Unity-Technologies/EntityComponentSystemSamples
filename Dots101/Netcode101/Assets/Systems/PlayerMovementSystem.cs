using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace KickBall
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct PlayerMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var playerConfig = SystemAPI.GetSingleton<PlayerConfig>();
            var speed = SystemAPI.Time.DeltaTime * playerConfig.Speed;

            foreach (var (playerTransform, input) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRO<PlayerInput>>()
                         .WithAll<Player, Simulate>())
            {
                if (input.ValueRO.Horizontal == 0 && input.ValueRO.Vertical == 0)
                {
                    continue;
                }
                
                // ... todo disallow player from walking through obstacles

                var move = new float3(input.ValueRO.Horizontal, 0, input.ValueRO.Vertical) * speed;
                playerTransform.ValueRW.Position += move;
            }
        }
    }
}
