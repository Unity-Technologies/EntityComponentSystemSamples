using Unity.Netcode;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

using Unity.Transforms;

// Tag added to new connections on the server once they have been initialized, so it's only done once
public struct ConnectionInitialized : IComponentData
{
}

// Connections are placed in game automatically once they have a NetworkId, so the server can spawn the player
// for a new connection right away, without waiting for the client to request it.
[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct InitializeConnectionServerSystem : ISystem
{
    private ComponentLookup<NetworkStreamIsReconnected> reconnectedLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CubeSpawner>();

        var builder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<NetworkId, NetworkStreamInGame>()
            .WithNone<ConnectionInitialized>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
        reconnectedLookup = state.GetComponentLookup<NetworkStreamIsReconnected>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Get the prefab to instantiate
        var prefab = SystemAPI.GetSingleton<CubeSpawner>().Cube;

        // Get the name of the prefab being instantiated
        state.EntityManager.GetName(prefab, out var prefabName);
        var worldName = state.WorldUnmanaged.Name;

        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        reconnectedLookup.Update(ref state);

        foreach (var (id, connection) in SystemAPI.Query<RefRO<NetworkId>>().WithAll<NetworkStreamInGame>().WithNone<ConnectionInitialized>().WithEntityAccess())
        {
            var networkId = id.ValueRO;
            commandBuffer.AddComponent<ConnectionInitialized>(connection);

            // If this is a reconnecting connection we don't need to spawn and configure a player entity
            // as it has been migrated to the new host and will be reconnected to this client
            if (reconnectedLookup.HasComponent(connection))
            {
                Debug.Log($"'{worldName}' connection '{networkId.Value}' has reconnected!");
                continue;
            }

            // Log information about the new connection that includes the client's assigned NetworkId and the name of the prefab spawned.
            Debug.Log($"'{worldName}' connection '{networkId.Value}' is in game, spawning a Ghost '{prefabName}' for them!");

            // Instantiate the prefab
            var player = commandBuffer.Instantiate(prefab);
            // Associate the instantiated prefab with the connected client's assigned NetworkId
            commandBuffer.SetComponent(player, new GhostOwner { NetworkId = networkId.Value});

            // Add the player to the linked entity group so it is destroyed automatically on disconnect
            commandBuffer.AppendToBuffer(connection, new LinkedEntityGroup{Value = player});

            // Give each NetworkId their own spawn pos:
            {
                var isEven = (networkId.Value & 1) == 0;
                const float halfCharacterWidthPlusHalfPadding = .55f;
                const float spawnStaggeredOffset = 0.25f;
                var staggeredXPos = networkId.Value * math.@select(halfCharacterWidthPlusHalfPadding, -halfCharacterWidthPlusHalfPadding, isEven) + math.@select(-spawnStaggeredOffset, spawnStaggeredOffset, isEven);
                var preventZFighting = -0.01f * networkId.Value;

                commandBuffer.SetComponent(player, LocalTransform.FromPosition(new float3(staggeredXPos, preventZFighting, 0)));
            }
        }
        commandBuffer.Playback(state.EntityManager);
    }
}
