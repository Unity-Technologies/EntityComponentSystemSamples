using Tutorials.Kickball.Execute;
using Tutorials.Kickball.Step1;
using Tutorials.Kickball.Step2;
using Tutorials.Kickball.Step3;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Tutorials.Kickball.Step5
{
    [UpdateBefore(typeof(BallMovementSystem))]
    public partial struct BallCarrySystem : ISystem
    {
        static readonly float3 CarryOffset = new float3(0, 2, 0);

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BallCarry>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();

            // move carried balls
            foreach (var (ballTransform, carrier) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRO<Carry>>()
                         .WithAll<Ball>())
            {
                var playerTransform = state.EntityManager.GetComponentData<LocalTransform>(carrier.ValueRO.Target);
                ballTransform.ValueRW.Position = playerTransform.Position + CarryOffset;
            }

            if (!Input.GetKeyDown(KeyCode.C))
            {
                return;
            }

            foreach (var (playerTransform, playerEntity) in
                     SystemAPI.Query<RefRW<LocalTransform>>()
                         .WithAll<Player>()
                         .WithEntityAccess())
            {
                if (state.EntityManager.IsComponentEnabled<Carry>(playerEntity))
                {
                    // put down ball
                    var carried = state.EntityManager.GetComponentData<Carry>(playerEntity);

                    var ballTransform = state.EntityManager.GetComponentData<LocalTransform>(carried.Target);
                    ballTransform.Position = playerTransform.ValueRO.Position;
                    state.EntityManager.SetComponentData(carried.Target, ballTransform);

                    state.EntityManager.SetComponentEnabled<Carry>(carried.Target, false);
                    state.EntityManager.SetComponentEnabled<Carry>(playerEntity, false);

                    state.EntityManager.SetComponentData(carried.Target, new Carry());
                    state.EntityManager.SetComponentData(playerEntity, new Carry());
                }
                else
                {
                    // pick up first ball in range
                    foreach (var (ballTransform, ballEntity) in
                             SystemAPI.Query<RefRO<LocalTransform>>()
                                 .WithAll<Ball>()
                                 .WithDisabled<Carry>()
                                 .WithEntityAccess())
                    {
                        float distSQ = math.distancesq(playerTransform.ValueRO.Position,
                            ballTransform.ValueRO.Position);

                        if (distSQ <= config.BallKickingRangeSQ)
                        {
                            state.EntityManager.SetComponentData(ballEntity, new Velocity());

                            state.EntityManager.SetComponentData(playerEntity, new Carry { Target = ballEntity });
                            state.EntityManager.SetComponentData(ballEntity, new Carry { Target = playerEntity });

                            state.EntityManager.SetComponentEnabled<Carry>(playerEntity, true);
                            state.EntityManager.SetComponentEnabled<Carry>(ballEntity, true);
                            break;
                        }
                    }
                }
            }
        }
    }
}
