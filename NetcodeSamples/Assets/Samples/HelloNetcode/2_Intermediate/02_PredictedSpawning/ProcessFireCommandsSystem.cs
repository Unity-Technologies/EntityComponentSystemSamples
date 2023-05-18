using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.NetCode;

namespace Samples.HelloNetcode
{
    // Counter for each fire command processed so it can alternate colors
    public struct FireCounter : IComponentData
    {
        public int Value;
    }

    // Handle the fire input events type specifically
    [UpdateInGroup(typeof(HelloNetcodePredictedSystemGroup))]
    [BurstCompile]
    public partial struct ProcessFireCommandsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnablePredictedSpawning>();
            state.RequireForUpdate<CharacterControllerPlayerInput>();
            state.RequireForUpdate<GrenadeSpawner>();
            state.EntityManager.CreateSingleton<FireCounter>();
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
            var fireCounter = SystemAPI.GetSingleton<FireCounter>();
            var fireCounterEntity = SystemAPI.GetSingletonEntity<FireCounter>();
            var localToWorldTransformLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            var time = state.WorldUnmanaged.Time.ElapsedTime;
            var isClient = state.WorldUnmanaged.IsClient();
            foreach (var (character, inputBuffer, anchorPoint) in SystemAPI.Query<CharacterAspect, DynamicBuffer<HelloNetcodeSamples.Generated.CharacterControllerPlayerInputInputBufferData>, RefRO<AnchorPoint>>().WithAll<Simulate>())
            {
                if (character.Input.SecondaryFire.IsSet)
                {
                    var grenadeEntity = commandBuffer.Instantiate(grenadePrefab);

                    // The spawn point is nested 3 levels deep on the player (slot->launcher->spawnPoint) but element 0 is the root entity
                    var spawnPointEntity = anchorPoint.ValueRO.SpawnPoint;
                    var grenadeSpawnPosition = localToWorldTransformLookup[spawnPointEntity].Position;
                    var grenadeSpawnRotation = localToWorldTransformLookup[spawnPointEntity].Rotation;

                    // Set the spawn location at the grenade spawn position and with it's rotation but in world coordinates since it's not a child of the player

                    commandBuffer.SetComponent(grenadeEntity, LocalTransform.FromPositionRotation(grenadeSpawnPosition, grenadeSpawnRotation));


                    // Launch the grenade by setting the physics linear velocity in the forward direction with the configured initial velocity
                    var initialVelocity = new PhysicsVelocity();
                    initialVelocity.Linear = localToWorldTransformLookup[anchorPoint.ValueRO.SpawnPoint].Forward * config.InitialVelocity;
                    commandBuffer.SetComponent(grenadeEntity, initialVelocity);

                    var grenadeData = new GrenadeData() {DestroyTimer = (float)time + config.BlastTimer};

                    // Set the spawn ID for this particular local spawn so it can be used later in the classification system
                    // Needs to include the network ID of the owner since everyone's counters/spawnId starts at 1
                    inputBuffer.GetDataAtTick(networkTime.ServerTick, out var inputForTick);
                    grenadeData.SpawnId = (uint)((uint)(inputForTick.InternalInput.PrimaryFire.Count << 11) | (uint)character.OwnerNetworkId);
                    commandBuffer.SetComponent(grenadeEntity, grenadeData);

                    // Set the owner so the prediction will work (important until it's replaced by it's interpolated version)
                    commandBuffer.SetComponent(grenadeEntity, new GhostOwner {NetworkId = character.OwnerNetworkId});

                    // Set the color on the grenade so it alternates between red and green
                    if (state.WorldUnmanaged.IsClient())
                    {
                        if (fireCounter.Value % 2 == 1)
                            commandBuffer.SetComponent(grenadeEntity, new URPMaterialPropertyBaseColor() { Value = new float4(1, 0, 0, 1) });
                        else
                            commandBuffer.SetComponent(grenadeEntity, new URPMaterialPropertyBaseColor() { Value = new float4(0, 1, 0, 1) });
                    }
                    commandBuffer.SetComponent(fireCounterEntity, new FireCounter(){Value = fireCounter.Value + 1});
                }
            }
            commandBuffer.Playback(state.EntityManager);
        }
    }
}
