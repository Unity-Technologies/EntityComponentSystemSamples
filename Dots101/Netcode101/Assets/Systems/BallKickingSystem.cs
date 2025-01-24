using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using KickBall;
using Unity.Physics;
using Unity.Physics.Extensions;

namespace Kickball
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct BallKickingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BallConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ballConfig = SystemAPI.GetSingleton<BallConfig>();
            
            foreach (var (input, playerTransform) in
                     SystemAPI.Query<RefRO<PlayerInput>, RefRO<LocalTransform>>()
                         .WithAll<Player, Simulate>())
            {
                if (!input.ValueRO.KickBall.IsSet)
                {
                    continue;
                }

                foreach (var (velocity, mass, ballTransform) in
                         SystemAPI.Query<RefRW<PhysicsVelocity>, RefRO<PhysicsMass>, RefRO<LocalTransform>>()
                             .WithAll<Ball>())
                {
                    float distSQ = math.distancesq(playerTransform.ValueRO.Position, ballTransform.ValueRO.Position);

                    if (distSQ <= ballConfig.KickingRangeSQ)
                    {
                        var playerToBall = ballTransform.ValueRO.Position.xz - playerTransform.ValueRO.Position.xz;
                        var impulse = (math.normalizesafe(playerToBall) * ballConfig.KickForce).xxy;
                        impulse.y = 0;
                        velocity.ValueRW.ApplyLinearImpulse(mass.ValueRO, impulse);
                    }
                }
            }
        }
    }
}
