using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Input = UnityEngine.Input;

namespace ActivationPlates
{
    public partial struct PlayerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ActivationPlates.Config>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();
            float3 input = new float3(Input.GetAxis($"Horizontal"), 0, Input.GetAxis($"Vertical"));

            var speed = config.PlayerMoveSpeed * SystemAPI.Time.DeltaTime;
            
            foreach (var playerTransform in
                     SystemAPI.Query<RefRW<LocalTransform>>()
                         .WithAll<Player>())
            {
                playerTransform.ValueRW.Position += input * speed;
            }
        }
    }
}