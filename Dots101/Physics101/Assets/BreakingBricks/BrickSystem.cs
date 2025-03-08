using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace BreakingBricks
{
    // the system runs after each iteration of collision detection and the solver
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    public partial struct BrickSystem : ISystem
    {
        private bool hasSpawnedBricks;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<BreakingBricks.Config>();
        }
 
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();

            // spawn bricks
            if (!hasSpawnedBricks)
            {
                hasSpawnedBricks = true;

                state.EntityManager.Instantiate(config.BrickPrefab, config.NumBricksSpawn, Allocator.Temp);

                var rand = new Random(123);

                foreach (var (brickTransform, color, collider) in
                         SystemAPI.Query<RefRW<LocalTransform>, RefRW<URPMaterialPropertyBaseColor>,
                                 RefRW<PhysicsCollider>>()
                             .WithAll<Brick>())
                {
                    var pos = rand.NextFloat3(config.SpawnBoundsMin, config.SpawnBoundsMax);
                    brickTransform.ValueRW.Position = pos;

                    color.ValueRW.Value = config.FullHitpointsColor;
                }
            }
            
            // needed to get the collision events
            var sim = SystemAPI.GetSingleton<SimulationSingleton>().AsSimulation();
            
            // to access the collisions events on main thread, we must sync any outstanding physics sim jobs
            sim.FinalJobHandle.Complete(); 
            
            // needed to get details of the collision events (estimated impulse)
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

            const float minImpactThreshold = 2f; // ignore impacts below this (to effectively ignore resting contacts) 
            var strengthModifier = config.ImpactStrength;
            
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var collisionEvent in sim.CollisionEvents)
            {
                // check if one of the two bodies is a brick and the other a ball
                Entity brickEntity;
                Entity ballEntity;
                
                // Note that, in a single physics update, a collision between a pair 
                // of bodies creates one collision event, not two.
                // The API makes no guarantee which body is EntityA and which is EntityB,
                // so you must test for both possibilities.
                
                if (SystemAPI.HasComponent<Brick>(collisionEvent.EntityA) &&
                    SystemAPI.HasComponent<Ball>(collisionEvent.EntityB))
                {
                    brickEntity = collisionEvent.EntityA;
                    ballEntity = collisionEvent.EntityB;
                }
                else if (SystemAPI.HasComponent<Brick>(collisionEvent.EntityB) &&
                         SystemAPI.HasComponent<Ball>(collisionEvent.EntityA))
                {
                    brickEntity = collisionEvent.EntityB;
                    ballEntity = collisionEvent.EntityA;
                }
                else
                {
                    continue;
                }
                
                var details = collisionEvent.CalculateDetails(ref physicsWorld);

                // ignore resting contacts
                if (details.EstimatedImpulse < minImpactThreshold)
                {
                    continue;
                }

                // reduce brick hitpoints
                var brick = SystemAPI.GetComponentRW<Brick>(brickEntity);
                brick.ValueRW.Hitpoints -= strengthModifier * details.EstimatedImpulse;

                // destroy brick if hitpoints below 0
                if (brick.ValueRO.Hitpoints <= 0)
                {
                    ecb.DestroyEntity(brickEntity);
                }
                else
                {
                    // update color of the hit brick
                    var color = SystemAPI.GetComponentRW<URPMaterialPropertyBaseColor>(brickEntity);
                    color.ValueRW.Value = math.lerp(config.EmptyHitpointsColor, config.FullHitpointsColor,
                        brick.ValueRO.Hitpoints);    
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}