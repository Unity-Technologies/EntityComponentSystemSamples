using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Unity.DotsUISample
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
    public partial struct PlayerMovementSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameData = SystemAPI.GetSingleton<GameData>();
            if (gameData.State != GameState.Questing)
            {
                return;
            }

            var playerMovement = GameInput.Move.ReadValue<Vector2>();

            foreach (var (player, playerTransform, velocity) in
                     SystemAPI.Query<RefRO<Player>, RefRW<LocalTransform>, RefRW<PhysicsVelocity>>())
            {
                if (math.lengthsq(playerMovement) > 0.01f)
                {
                    float3 moveDirection = math.normalizesafe(new float3(playerMovement.x, 0, playerMovement.y));
                    velocity.ValueRW.Linear = moveDirection * player.ValueRO.MovementSpeed;
                    playerTransform.ValueRW.Rotation = math.slerp(
                        playerTransform.ValueRW.Rotation,
                        quaternion.LookRotationSafe(moveDirection, math.up()),
                        10f * SystemAPI.Time.DeltaTime
                    );
                }
                else
                {
                    velocity.ValueRW.Linear = math.lerp(velocity.ValueRO.Linear, float3.zero, 10f * SystemAPI.Time.DeltaTime);
                }

                playerTransform.ValueRW.Rotation.value.xz = 0f;
                playerTransform.ValueRW.Position.y = 0f;
            }
        }
    }
}