using Tutorials.Kickball.Execute;
using Tutorials.Kickball.Step1;
using Tutorials.Kickball.Step2;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Tutorials.Kickball.Step3
{
    // This UpdateBefore is necessary to ensure the balls get rendered in
    // the correct position for the frame in which they're spawned.
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct BallSpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BallSpawner>();
            state.RequireForUpdate<Config>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();

            if (!Input.GetKeyDown(KeyCode.Return))
            {
                return;
            }

            var rand = new Random(123);

            // For every player, spawn a ball, position it at the player's location, and give it a random velocity.
            foreach (var transform in
                     SystemAPI.Query<RefRO<LocalTransform>>()
                         .WithAll<Player>())
            {
                var ball = state.EntityManager.Instantiate(config.BallPrefab);
                state.EntityManager.SetComponentData(ball, new LocalTransform
                {
                    Position = transform.ValueRO.Position,
                    Rotation = quaternion.identity,
                    Scale = 1
                });
                state.EntityManager.SetComponentData(ball, new Velocity
                {
                    // NextFloat2Direction() returns a random 2d unit vector.
                    Value = rand.NextFloat2Direction() * config.BallStartVelocity
                });
            }
        }
    }
}
