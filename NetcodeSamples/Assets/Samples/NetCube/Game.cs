﻿using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

// RPC request from client to server for game to go "in game" and send snapshots / inputs
public struct GoInGameRequest : IRpcCommand
{
}

// When client has a connection with network id, go in game and tell server to also go in game
[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
public partial struct GoInGameClientSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Run only on entities with a CubeSpawner component data
        state.RequireForUpdate<CubeSpawner>();

        var builder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<NetworkId>()
            .WithNone<NetworkStreamInGame>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithEntityAccess().WithNone<NetworkStreamInGame>())
        {
            commandBuffer.AddComponent<NetworkStreamInGame>(entity);
            var req = commandBuffer.CreateEntity();
            commandBuffer.AddComponent<GoInGameRequest>(req);
            commandBuffer.AddComponent(req, new SendRpcCommandRequest { TargetConnection = entity });
        }
        commandBuffer.Playback(state.EntityManager);
    }
}

// When server receives go in game request, go in game and delete request
[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct GoInGameServerSystem : ISystem
{
    private ComponentLookup<NetworkId> networkIdLookup;
    private ComponentLookup<NetworkStreamIsReconnected> reconnectedLookup;
    private ComponentLookup<CubeColor> cubeColorLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CubeSpawner>();

        var builder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<GoInGameRequest>()
            .WithAll<ReceiveRpcCommandRequest>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
        networkIdLookup = state.GetComponentLookup<NetworkId>(true);
        reconnectedLookup = state.GetComponentLookup<NetworkStreamIsReconnected>(true);
        cubeColorLookup = state.GetComponentLookup<CubeColor>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Get the prefab to instantiate
        var prefab = SystemAPI.GetSingleton<CubeSpawner>().Cube;

        // Get the cube config for setting the position of the new cube
        SystemAPI.TryGetSingletonRW<CubeConfig>(out var config);

        // Ge the name of the prefab being instantiated
        state.EntityManager.GetName(prefab, out var prefabName);
        var worldName = state.WorldUnmanaged.Name;

        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        networkIdLookup.Update(ref state);
        reconnectedLookup.Update(ref state);
        cubeColorLookup.Update(ref state);

        foreach (var (reqSrc, reqEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<GoInGameRequest>().WithEntityAccess())
        {
            // Get the NetworkId for the requesting client
            var networkId = networkIdLookup[reqSrc.ValueRO.SourceConnection];

            // If this request is coming from a reconnecting connection we don't need to spawn and configure a player entity
            // as it has been migrated to the new host and will be reconnected to this client
            if (reconnectedLookup.HasComponent(reqSrc.ValueRO.SourceConnection))
            {
                Debug.Log($"'{worldName}' connection '{networkId.Value}' has reconnected!");
                commandBuffer.DestroyEntity(reqEntity);
                continue;
            }

            commandBuffer.AddComponent<NetworkStreamInGame>(reqSrc.ValueRO.SourceConnection);

            // Log information about the connection request that includes the client's assigned NetworkId and the name of the prefab spawned.
            Debug.Log($"'{worldName}' setting connection '{networkId.Value}' to in game, spawning a Ghost '{prefabName}' for them!");

            // Instantiate the prefab
            var player = commandBuffer.Instantiate(prefab);
            // Associate the instantiated prefab with the connected client's assigned NetworkId
            commandBuffer.SetComponent(player, new GhostOwner { NetworkId = networkId.Value});

            // The connection has the color which this player owns, this will be applied to the ship on each spawn
            commandBuffer.SetComponent(player, new CubeColor {Value = cubeColorLookup[reqSrc.ValueRO.SourceConnection].Value});

            // Add the player to the linked entity group so it is destroyed automatically on disconnect
            commandBuffer.AppendToBuffer(reqSrc.ValueRO.SourceConnection, new LinkedEntityGroup{Value = player});

            // Give each new player their own spawn pos:
            {
                var positionIndex = 0;
                if (config.IsValid)
                    positionIndex = config.ValueRW.NextPositionValue++;
                var isEven = (positionIndex & 1) == 0;
                const float halfCharacterWidthPlusHalfPadding = .55f;
                const float spawnStaggeredOffset = 0.25f;
                var staggeredXPos = positionIndex * math.@select(halfCharacterWidthPlusHalfPadding, -halfCharacterWidthPlusHalfPadding, isEven) + math.@select(-spawnStaggeredOffset, spawnStaggeredOffset, isEven);
                var preventZFighting = -0.01f * positionIndex;

                commandBuffer.SetComponent(player, LocalTransform.FromPosition(new float3(staggeredXPos, preventZFighting, 0)));
            }
            commandBuffer.DestroyEntity(reqEntity);
        }
        commandBuffer.Playback(state.EntityManager);
    }
}
