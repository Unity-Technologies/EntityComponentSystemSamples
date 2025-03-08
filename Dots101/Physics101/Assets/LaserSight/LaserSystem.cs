using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace LaserSight
{
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public partial struct LaserSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LaserSight.Config>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        // we cannot Burst compile this update because it accesses managed objects
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();

            // move the player
            {
                float3 input = new float3(Input.GetAxis($"Horizontal"), 0, Input.GetAxis($"Vertical"));
                var speed = config.PlayerMoveSpeed * SystemAPI.Time.DeltaTime;
                foreach (var playerTransform in
                         SystemAPI.Query<RefRW<LocalTransform>>()
                             .WithAll<Player>())
                {
                    playerTransform.ValueRW.Position += input * speed;
                }
            }
            
            float laserLength = 0;

            // raycast to determine the laser length
            {
                // to perform raycasts or other collision queries, we need the collision world
                var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
                
                foreach (var playerTransform in
                         SystemAPI.Query<RefRO<LocalTransform>>()
                             .WithAll<Player>())
                {
                    var raycast = new RaycastInput
                    {
                        // specify starting and end points of the raycast (which together imply direction)
                        Start = playerTransform.ValueRO.Position,
                        End = playerTransform.ValueRO.Position + new float3(0, 0, config.MaxLaserLength),
                        // don't forget to set a filter or else you'll get no hits!
                        Filter = CollisionFilter.Default
                    };

                    if (collisionWorld.CastRay(raycast, out var closestHit))
                    {
                        // set laser length to the distance of the closest hit
                        laserLength = math.distance(playerTransform.ValueRO.Position, closestHit.Position);
                    }
                    else
                    {
                        // no hit detected, so just set the laser to max length
                        laserLength = config.MaxLaserLength;
                    }
                }
            }

            // set the laser endpoints
            {
                foreach (var (playerTransform, player) in
                         SystemAPI.Query<RefRW<LocalTransform>, RefRW<Player>>())
                {
                    // init the laser ref
                    if (!player.ValueRW.Laser.IsValid())
                    {
                        player.ValueRW.Laser = GameObject.FindFirstObjectByType<LineRenderer>();
                    }

                    var laser = player.ValueRO.Laser.Value;
                    laser.SetPosition(0, playerTransform.ValueRO.Position);
                    laser.SetPosition(1, playerTransform.ValueRO.Position + new float3(0, 0, laserLength));
                }
            }
        }
    }
}