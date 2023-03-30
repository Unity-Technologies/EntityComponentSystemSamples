using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Samples.HelloNetcode
{
    // Timer to track when to destroy the explosion effect left by a grenade
    public struct ExplosionData : IComponentData
    {
        public float Timer;
    }

    // Track the position of grenades on clients here, to play the explosion effect when they are destroyed (this
    // will also delete the transform data and thus it needs recording separately)
    public struct GrenadeClientCleanupData : ICleanupComponentData
    {
        public float3 Position;
    }

    // Server only system which handles the grenade behaviour, destroy when timer runs out and push close physics objects away
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(HelloNetcodePredictedSystemGroup))]
    [UpdateAfter(typeof(GrenadeLauncherSystem))]
    [BurstCompile]
    public partial struct GrenadeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnablePredictedSpawning>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var time = state.WorldUnmanaged.Time.ElapsedTime;
            var config = SystemAPI.GetSingleton<GrenadeConfig>();
            foreach (var (data, grenadeTransform, entity) in SystemAPI.Query<RefRO<GrenadeData>, RefRO<LocalTransform>>().WithAll<Simulate>().WithEntityAccess())
            {
                // Destroy the grenade when it reaches the end of it's timer
                if (time > data.ValueRO.DestroyTimer)
                {
                    // Calculate which objects are within the blast radius of the grenade and apply explosion effect on
                    // them based on distance. The further away the grenade the less affected by the blast.
                    foreach (var (velocity, transform) in SystemAPI.Query<RefRW<PhysicsVelocity>, RefRO<LocalTransform>>().WithAll<Simulate>())
                    {
                        var diff = transform.ValueRO.Position - grenadeTransform.ValueRO.Position;
                        var distanceSqrt = math.lengthsq(diff);
                        if (distanceSqrt < config.BlastRadius && distanceSqrt != 0)
                        {
                            var scaledPower = 1.0f - distanceSqrt / config.BlastRadius;
                            velocity.ValueRW.Linear = config.BlastPower * scaledPower * (diff / math.sqrt(distanceSqrt));
                        }
                    }
                    commandBuffer.DestroyEntity(entity);
                }
            }
            commandBuffer.Playback(state.EntityManager);
        }
    }

    [UpdateInGroup(typeof(HelloNetcodePredictedSystemGroup))]
    // Handle the rotation (up/down) of the grenade launcher, runs on both client and server as this is required
    // to figure out the spawn point of the grenade
    [BurstCompile]
    public partial struct GrenadeLauncherSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnablePredictedSpawning>();
            state.RequireForUpdate<NetworkId>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach (var (character, anchorPoint) in SystemAPI.Query<CharacterAspect, RefRO<AnchorPoint>>().WithAll<Simulate>())
            {
                // This is the weapon slot and rotating that will make the launcher move correctly (it's anchored on the end)
                var grenadeLauncher = anchorPoint.ValueRO.WeaponSlot;
                var followCameraRotation = quaternion.RotateX(-character.Input.Pitch);

                var transform = state.EntityManager.GetComponentData<LocalTransform>(grenadeLauncher);
                commandBuffer.SetComponent(grenadeLauncher, transform.WithRotation(followCameraRotation));

            }
            commandBuffer.Playback(state.EntityManager);
        }
    }

    // Handle client only behaviour needed to support the grenade explosions
    [UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [BurstCompile]
    public partial struct ExplosionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnablePredictedSpawning>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var time = state.WorldUnmanaged.Time.ElapsedTime;
            var explosionPrefab = SystemAPI.GetSingleton<GrenadeSpawner>().Explosion;
            var config = SystemAPI.GetSingleton<GrenadeConfig>();

            // Add the grenade cleanup component to all grenades when they've arrived in a ghost snapshot (so server has also spawned
            // it), otherwise we might track invalid mis-predicted ghost spawns
            foreach (var (grenadeData,entity) in SystemAPI.Query<RefRO<GrenadeData>>().WithNone<PredictedGhostSpawnRequest>().WithNone<GrenadeClientCleanupData>().WithEntityAccess())
            {
                commandBuffer.AddComponent<GrenadeClientCleanupData>(entity);
            }

            // Record the grenade position every frame so it can be used when instantiating the explosion effect
            foreach (var (transform, grenadeData) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<GrenadeClientCleanupData>>())
            {
                grenadeData.ValueRW.Position = transform.ValueRO.Position;
            }

            // When a grenade is destroyed (GrenadeData deleted) we'll still see the system component and spawn an explosion
            // effect based on that
            foreach (var (grenade, entity) in SystemAPI.Query<RefRO<GrenadeClientCleanupData>>().WithNone<GrenadeData>().WithEntityAccess())
            {
                var explosion = commandBuffer.Instantiate(explosionPrefab);

                commandBuffer.SetComponent(explosion, LocalTransform.FromPosition(grenade.ValueRO.Position));

                commandBuffer.AddComponent(explosion, new ExplosionData(){Timer = (float)time + config.ExplosionTimer});
                commandBuffer.RemoveComponent<GrenadeClientCleanupData>(entity);
            }

            // The explosion particle systems need to be destroyed manually when one loop has finished
            foreach (var (explosionData, entity) in SystemAPI.Query<RefRO<ExplosionData>>().WithAll<Simulate>().WithEntityAccess())
            {
                if (explosionData.ValueRO.Timer < time)
                    commandBuffer.DestroyEntity(entity);
            }
            commandBuffer.Playback(state.EntityManager);
        }
    }
}
