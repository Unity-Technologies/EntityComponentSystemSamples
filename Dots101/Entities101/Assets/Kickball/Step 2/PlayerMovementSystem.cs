using Tutorials.Kickball.Execute;
using Tutorials.Kickball.Step1;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Tutorials.Kickball.Step2
{
    public partial struct PlayerMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerMovement>();
            state.RequireForUpdate<Config>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();

            // Get directional input. (Most of the UnityEngine.Input are Burst compatible, but not all are.
            // If the OnUpdate, OnCreate, or OnDestroy methods needs to access managed objects or call methods
            // that aren't Burst-compatible, the [BurstCompile] attribute can be omitted.
            var horizontal = Input.GetAxis("Horizontal");
            var vertical = Input.GetAxis("Vertical");
            var input = new float3(horizontal, 0, vertical) * SystemAPI.Time.DeltaTime * config.PlayerSpeed;

            // If there's no directional input this frame, we don't need to move the players.
            if (input.Equals(float3.zero))
            {
                return;
            }

            var minDist = config.ObstacleRadius + 0.5f; // the player capsule radius is 0.5f
            var minDistSQ = minDist * minDist;

            // For every entity having a LocalTransform and Player component, a read-write reference to
            // the LocalTransform is assigned to 'playerTransform'.
            foreach (var playerTransform in
                     SystemAPI.Query<RefRW<LocalTransform>>()
                         .WithAll<Player>())
            {
                var newPos = playerTransform.ValueRO.Position + input;

                // A foreach query nested inside another foreach query.
                // For every entity having a LocalTransform and Obstacle component, a read-only reference to
                // the LocalTransform is assigned to 'obstacleTransform'.
                foreach (var obstacleTransform in
                         SystemAPI.Query<RefRO<LocalTransform>>()
                             .WithAll<Obstacle>())
                {
                    // If the new position intersects the player with a wall, don't move the player.
                    if (math.distancesq(newPos, obstacleTransform.ValueRO.Position) <= minDistSQ)
                    {
                        newPos = playerTransform.ValueRO.Position;
                        break;
                    }
                }

                playerTransform.ValueRW.Position = newPos;
            }
        }
    }
}
