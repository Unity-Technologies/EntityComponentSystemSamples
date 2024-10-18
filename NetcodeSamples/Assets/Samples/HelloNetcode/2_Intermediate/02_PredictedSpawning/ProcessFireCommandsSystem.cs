using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.NetCode;

namespace Samples.HelloNetcode
{
    // Handle the fire input events type specifically
    [UpdateInGroup(typeof(HelloNetcodePredictedSystemGroup))]
    [BurstCompile]
    public partial struct ProcessFireCommandsSystem : ISystem
    {
        private ComponentLookup<LocalTransform> m_TransformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnablePredictedSpawning>();
            state.RequireForUpdate<CharacterControllerPlayerInput>();
            state.RequireForUpdate<GrenadeSpawner>();
            m_TransformLookup = state.GetComponentLookup<LocalTransform>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Since this is only handling the fire inputs which spawn grenades we'll only want to predict the spawn one time
            // (or we'd get lots of grenades spawned for one instance as the prediction for a tick can run multiple times)
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!networkTime.IsFirstTimeFullyPredictingTick)
                return;

            var config = SystemAPI.GetSingleton<GrenadeConfig>();
            var commandBuffer = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var grenadePrefab = SystemAPI.GetSingleton<GrenadeSpawner>().Grenade;
            var localToWorldTransformLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            var time = state.WorldUnmanaged.Time;
            m_TransformLookup.Update(ref state);

            state.CompleteDependency();
            var originalGranadeScale = m_TransformLookup[grenadePrefab].Scale;

            foreach (var (character, inputBuffer, anchorPoint) in SystemAPI.Query<CharacterAspect,
                         DynamicBuffer<InputBufferData<CharacterControllerPlayerInput>>, RefRO<AnchorPoint>>().WithAll<Simulate>())
            {

                // We must fetch the quantity of grenades to spawn from the Count value of the SecondaryFire InputEvent (which stores this exact delta).
                // Why? Because:
                // - Users may drop packets, so a users click counter can increment more than once (representing that they've clicked on previous 'not yet acked' ticks).
                // - Users may be clicking repeatedly in partial ticks. Imagine a SimulationTickRate of 10. It's possible to click faster than once per 100ms, so we must count 2 in one Simulation tick.
                // - The server may be batching ticks together (due to perf issues).
                var grenadesToSpawn = character.Input.SecondaryFire.Count;
                if (grenadesToSpawn <= 0) continue;

                // Get the ABSOLUTE counter value now, as we need it later:
                inputBuffer.GetDataAtTick(networkTime.ServerTick, out var currentInput);

                // In a real game, you'll rate-limit these kinds of player actions via game design choices.
                // E.g. throwing a grenade would have a maximum rate of fire, regardless of how often the user presses the button.
                // But for this sample, we're illustrating EXACTLY one grenade spawned per right-click (ignoring held press).
                const int maxGrenadesPerPlayerPerServerTick = 5;
                if (grenadesToSpawn > maxGrenadesPerPlayerPerServerTick)
                {
                    SystemAPI.GetSingleton<NetDebug>().Log($"Clamping player input, as they're attempting to spawn {grenadesToSpawn} grenades in one tick (max: {maxGrenadesPerPlayerPerServerTick})!");
                    grenadesToSpawn = maxGrenadesPerPlayerPerServerTick;
                }

                // Batch Instantiate:
                using var grenadeEntities = new NativeArray<Entity>((int) grenadesToSpawn, Allocator.Temp);
                commandBuffer.Instantiate(grenadePrefab, grenadeEntities);

                for (int spawnId = 0; spawnId < grenadesToSpawn; spawnId++)
                {
                    var grenadeEntity = grenadeEntities[spawnId];

                    // Note: As the component stores the delta since the previous tick, we must fetch the absolute counter
                    // value (used for classification) from the actual buffer. Combining them, we can reconstruct previous
                    // SpawnId's, allowing us to perfectly match our server spawns with the clients predicted spawns.
                    uint secondaryFireCount = (uint) (currentInput.InternalInput.SecondaryFire.Count - spawnId);

                    // The spawn point is nested 3 levels deep on the player (slot->launcher->spawnPoint) but element 0 is the root entity
                    var spawnPointEntity = anchorPoint.ValueRO.SpawnPoint;
                    var grenadeSpawnPosition = localToWorldTransformLookup[spawnPointEntity].Position;
                    var grenadeSpawnRotation = localToWorldTransformLookup[spawnPointEntity].Rotation;
                    var granadeSpawnScale = originalGranadeScale;

                    // Launch the grenade by setting the physics linear velocity in the forward direction with the configured initial velocity
                    var initialVelocity = new PhysicsVelocity();
                    initialVelocity.Linear = localToWorldTransformLookup[anchorPoint.ValueRO.SpawnPoint].Forward * config.InitialVelocity;

                    // Offset the spawn position by it's velocity * a fraction of a tick (based on spawnID), so that,
                    // if we're spawning multiple grenades in one frame, they're offset from each other.
                    var spawnIdFraction = (float) spawnId / maxGrenadesPerPlayerPerServerTick;
                    grenadeSpawnPosition += (spawnIdFraction * time.DeltaTime) * initialVelocity.Linear;

                    // Set the spawn location at the grenade spawn position and with it's rotation but in world coordinates since it's not a child of the player
                    commandBuffer.SetComponent(grenadeEntity, LocalTransform.FromPositionRotationScale(grenadeSpawnPosition, grenadeSpawnRotation, granadeSpawnScale));
                    commandBuffer.SetComponent(grenadeEntity, initialVelocity);

                    var grenadeData = new GrenadeData() {DestroyTimer = (float) time.ElapsedTime + config.BlastTimer};

                    // Set the spawn ID for this particular local spawn so it can be used later in the classification system
                    // Needs to include the network ID of the owner since everyone's counters/spawnId starts at 1
                    grenadeData.SpawnId = (uint) character.OwnerNetworkId << 16 | secondaryFireCount;
                    commandBuffer.SetComponent(grenadeEntity, grenadeData);

                    // Set the owner so the prediction will work (important until it's replaced by it's interpolated version)
                    commandBuffer.SetComponent(grenadeEntity, new GhostOwner {NetworkId = character.OwnerNetworkId});
                }
            }
            commandBuffer.Playback(state.EntityManager);
        }
    }
}
