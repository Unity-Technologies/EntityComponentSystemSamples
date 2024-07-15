using Unity.Burst;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Samples.HelloNetcode
{
    /// <summary>Denotes an entity with an explosion ParticleSystem.</summary>
    public struct ExplosionParticleSystem : IComponentData
    {
    }

    /// <summary>Handles the grenade behaviour, destroy when timer runs out and push close physics objects away.</summary>
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
            var time = state.WorldUnmanaged.Time;
            var config = SystemAPI.GetSingleton<GrenadeConfig>();
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            var explosionPrefab = SystemAPI.GetSingleton<GrenadeSpawner>().Explosion;
            var isServer = state.WorldUnmanaged.IsServer();

            foreach (var (data, grenadeTransform, grenade) in SystemAPI.Query<RefRO<GrenadeData>, RefRO<LocalTransform>>().WithAll<Simulate>().WithNone<DisableRendering>().WithEntityAccess())
            {
                // Destroy the grenade when it reaches the end of it's timer
                if (time.ElapsedTime > data.ValueRO.DestroyTimer)
                {
                    // Calculate which objects are within the blast radius of the grenade and apply explosion effect on
                    // them based on distance. The further away the grenade the less affected by the blast.
                    foreach (var (velocity, grenadeData, transform) in SystemAPI.Query<RefRW<PhysicsVelocity>, RefRW<GrenadeData>, RefRO<LocalTransform>>().WithAll<Simulate>())
                    {
                        var diff = transform.ValueRO.Position - grenadeTransform.ValueRO.Position;
                        var distanceSqrt = math.lengthsq(diff);
                        if (distanceSqrt < config.BlastRadius && distanceSqrt != 0)
                        {
                            var scaledPower = 1.0f - distanceSqrt / config.BlastRadius;
                            // Add some verticality to the explosion, biasing towards world.up.
                            diff.y = diff.y >= -0.05f ? math.max(config.BlastPowerClampY, diff.y) : math.min(-config.BlastPowerClampY, diff.y);
                            velocity.ValueRW.Linear = config.BlastPower * scaledPower * (diff / math.sqrt(distanceSqrt));

                            // Also cause them to 'chain-react' as it looks cool (but not instantly, unless it was already going to):
                            grenadeData.ValueRW.DestroyTimer = math.min(grenadeData.ValueRW.DestroyTimer, (float)time.ElapsedTime + config.ChainReactionForceExplodeDurationSeconds);
                        }
                    }

                    if (isServer)
                    {
                        // Destroy the grenade on the server:
                        commandBuffer.DestroyEntity(grenade);
                    }
                    else
                    {
                        // Spawn the explosion VFX on the client:
                        if (networkTime.IsFirstTimeFullyPredictingTick)
                        {
                            var explosion = commandBuffer.Instantiate(explosionPrefab);
                            commandBuffer.SetComponent(explosion, LocalTransform.FromPosition(grenadeTransform.ValueRO.Position));
                            commandBuffer.AddComponent<ExplosionParticleSystem>(explosion);
                            // Hide it, and in doing so, prevent re-triggering (see above query filter).
                            commandBuffer.AddComponent<DisableRendering>(grenade);
                        }
                    }
                }
            }
            commandBuffer.Playback(state.EntityManager);
        }
    }

    /// <summary>Destroy expired explosion particle systems.</summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [BurstCompile]
    public partial struct ExplosionSystem : ISystem
    {
        private EntityQuery m_ParticleSystemQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnablePredictedSpawning>();
            m_ParticleSystemQuery = state.GetEntityQuery(typeof(ParticleSystem), typeof(ExplosionParticleSystem));
            state.RequireForUpdate(m_ParticleSystemQuery);
        }

        // Not burst compatible, as iterates over ParticleSystems!
        public void OnUpdate(ref SystemState state)
        {
            var time = state.WorldUnmanaged.Time;
            var particleSystemEntities = m_ParticleSystemQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in particleSystemEntities)
            {
                // Hack: Only required because ParticleSystem's on Entities don't automatically self-destruct.
                var ps = state.EntityManager.GetComponentObject<ParticleSystem>(entity);
                Debug.Assert(ps.main.stopAction == ParticleSystemStopAction.Destroy);
                if (ps.time + (time.DeltaTime * 2) > ps.main.duration)
                    state.EntityManager.DestroyEntity(entity);
            }
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
}
