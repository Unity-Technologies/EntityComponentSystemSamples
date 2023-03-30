using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Unity.NetCode.Samples.PlayerList
{
    /// <summary>
    ///     Manages the <see cref="PlayerListEntry" /> component and RPC's, which allows clients to view the names and
    ///     connection statuses of other clients.
    /// </summary>
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerPlayerListSystem : ISystem
    {
        EntityArchetype m_InvalidUsernameRpcArchetype;
        EntityArchetype m_RpcArchetype;
        EntityQuery m_PlayerListQuery;
        EntityQuery m_NewNetworkStreamConnectionsQuery;
        ComponentLookup<PlayerListEntry> m_PlayerListEntryFromEntity;
        ComponentLookup<NetworkId> m_NetworkIdFromEntity;
        EntityQuery m_ClientRegisterUsernameRpcQuery;
        EntityQuery m_DisconnectsQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_PlayerListQuery = state.GetEntityQuery(ComponentType.ReadOnly<PlayerListEntry>());
            using var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkStreamConnection>()
                .WithNone<ConnectionState>();
            m_NewNetworkStreamConnectionsQuery = state.GetEntityQuery(builder);

            var archetypeTypes = new NativeArray<ComponentType>(2, Allocator.Temp);
            archetypeTypes[0] = ComponentType.ReadOnly<PlayerListEntry.ChangedRpc>();
            archetypeTypes[1] = ComponentType.ReadOnly<SendRpcCommandRequest>();
            m_RpcArchetype = state.EntityManager.CreateArchetype(archetypeTypes);
            archetypeTypes[0] = ComponentType.ReadOnly<PlayerListEntry.InvalidUsernameResponseRpc>();
            m_InvalidUsernameRpcArchetype = state.EntityManager.CreateArchetype(archetypeTypes);

            archetypeTypes[0] = ComponentType.ReadWrite<PlayerListEntry>();
            archetypeTypes[1] = ComponentType.ReadOnly<ConnectionState>();
            m_DisconnectsQuery = state.GetEntityQuery(archetypeTypes);
            m_DisconnectsQuery.AddChangedVersionFilter(archetypeTypes[1]);
            archetypeTypes.Dispose();

            m_PlayerListEntryFromEntity = state.GetComponentLookup<PlayerListEntry>(true);
            m_NetworkIdFromEntity = state.GetComponentLookup<NetworkId>(true);

            m_ClientRegisterUsernameRpcQuery = state.GetEntityQuery(ComponentType.ReadOnly<PlayerListEntry.ClientRegisterUsernameRpc>());


            state.RequireForUpdate<EnablePlayerListsFeature>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rpcArchetype = m_RpcArchetype;
            var invalidUsernameRpcArchetype = m_InvalidUsernameRpcArchetype;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var netDbg = SystemAPI.GetSingleton<NetDebug>();

            // Ensure every NetworkConnection also has a ConnectionState so we can track disconnects.
            state.EntityManager.AddComponent<ConnectionState>(m_NewNetworkStreamConnectionsQuery);

            if (!m_DisconnectsQuery.IsEmpty)
            {
                state.Dependency = new NotifyPlayersOfDisconnectsJob
                {
                    netDbg = netDbg,
                    ecb = ecb,
                    rpcArchetype = rpcArchetype,
                }.Schedule(m_DisconnectsQuery, state.Dependency); // TODO: When error is fixed, remove the manual query.
            }

            if (!m_ClientRegisterUsernameRpcQuery.IsEmptyIgnoreFilter)
            {
                // We only add new players IF they send us a username. This ensures that they will always have a valid username from the start.
                m_PlayerListEntryFromEntity.Update(ref state);
                m_NetworkIdFromEntity.Update(ref state);

                var playerListEntries = m_PlayerListQuery.ToComponentDataListAsync<PlayerListEntry>(state.WorldUpdateAllocator, out var gatherPlayerListsHandle);
                var dependency = JobHandle.CombineDependencies(state.Dependency, gatherPlayerListsHandle);
                state.Dependency = new HandleNewJoinersJob
                {
                    ecb = ecb,
                    netDbg = netDbg,
                    rpcArchetype = rpcArchetype,
                    invalidUsernameRpcArchetype = invalidUsernameRpcArchetype,
                    playerListEntries = m_PlayerListEntryFromEntity,
                    networkIds = m_NetworkIdFromEntity,
                    existingPlayerListEntries = playerListEntries,
                }.Schedule(dependency);
            }
        }

        [BurstCompile]
        public partial struct HandleNewJoinersJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public EntityArchetype rpcArchetype;
            public EntityArchetype invalidUsernameRpcArchetype;
            public NetDebug netDbg;
            [ReadOnly]
            public ComponentLookup<PlayerListEntry> playerListEntries;
            [ReadOnly]
            public ComponentLookup<NetworkId> networkIds;
            [ReadOnly]
            public NativeList<PlayerListEntry> existingPlayerListEntries;

            public void Execute(Entity rpcEntity, ref PlayerListEntry.ClientRegisterUsernameRpc rpc, in ReceiveRpcCommandRequest req)
            {
                var networkId = networkIds[req.SourceConnection];

                // Auto-patch here rather than kicking the player as players don't pick their default names.
                var originalUsername = rpc.Value;
                rpc.Value = UsernameSanitizer.SanitizeUsername(rpc.Value, networkId.Value, out var usernameWasSanitized);

                if (usernameWasSanitized)
                    netDbg.LogError($"Server received a PlayerListEntry.ClientRegisterUsernameRpc with an invalid username '{originalUsername}', sanitized to '{rpc.Value}'!");

                // Note that a ClientRegisterUsernameRpc can mean either a:
                if (!playerListEntries.TryGetComponent(req.SourceConnection, out var entry))
                {
                    // NEW JOINER:
                    entry.State = new PlayerListEntry.ChangedRpc
                    {
                        ChangeType = PlayerListEntry.ChangedRpc.UpdateType.NewJoiner,
                        Reason = default,
                        NetworkId = networkId.Value,
                        Username = rpc
                    };

                    ecb.AddComponent(req.SourceConnection, entry);
                    NotifyJoinerOfAllExistingPlayers(ref existingPlayerListEntries, ref ecb, in rpcArchetype, in req.SourceConnection);
                }
                else
                {
                    // EXISTING PLAYER with a new username:
                    if (entry.State.Username.Value == rpc.Value)
                    {
                        netDbg.LogWarning($"Server received a PlayerListEntry.ChangedRpc from existing player {entry.State.NetworkId} but username '{rpc.Value}' is identical to cached value. Ignoring.");
                        ecb.DestroyEntity(rpcEntity);
                        return;
                    }

                    netDbg.DebugLog($"Server received a PlayerListEntry.ChangedRpc from an already connected player {entry.State.NetworkId}! Broadcasting the rename ('{entry.State.Username.Value}' >>> '{rpc.Value}').");
                    entry.State.ChangeType = PlayerListEntry.ChangedRpc.UpdateType.UsernameChange;
                    entry.State.Username = rpc;

                    // Update the Servers cached entry.
                    ecb.SetComponent(req.SourceConnection, entry);
                }

                // Broadcast notify of username by re-purposing the RPC.
                // We only need to broadcast if it's pertinent to other clients:
                // Otherwise we just send back to sender.
                ecb.RemoveComponent<PlayerListEntry.ClientRegisterUsernameRpc>(rpcEntity);
                ecb.RemoveComponent<ReceiveRpcCommandRequest>(rpcEntity);

                ecb.AddComponent(rpcEntity, entry.State);
                ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);

                // Notify the sender that their original became this new, sanitized input, so that they can accept the servers value.
                if (usernameWasSanitized)
                {
                    var clientInvalidUsernameRpc = ecb.CreateEntity(invalidUsernameRpcArchetype);
                    ecb.AddComponent(clientInvalidUsernameRpc, new PlayerListEntry.InvalidUsernameResponseRpc
                    {
                        RequestedUsername = originalUsername
                    });
                    ecb.SetComponent(clientInvalidUsernameRpc, new SendRpcCommandRequest
                    {
                        TargetConnection = req.SourceConnection
                    });
                }
            }
        }

        [BurstCompile]
        public partial struct NotifyPlayersOfDisconnectsJob : IJobEntity
        {
            public NetDebug netDbg;
            public EntityCommandBuffer ecb;
            public EntityArchetype rpcArchetype;

            public void Execute(Entity netIdEntity, ref PlayerListEntry entry, in ConnectionState connectionState)
            {
                if (connectionState.CurrentState == ConnectionState.State.Disconnected)
                {
                    netDbg.DebugLog($"Server: Established player {connectionState.NetworkId} disconnected with reason {connectionState.DisconnectReason}! Notifying other players.");
                    entry.State.Reason = connectionState.DisconnectReason;
                    entry.State.ChangeType = PlayerListEntry.ChangedRpc.UpdateType.PlayerDisconnect;

                    // Broadcast notify of state:
                    var rpcEntity = ecb.CreateEntity(rpcArchetype);
                    ecb.SetComponent(rpcEntity, entry.State);

                    // Cleanup.
                    ecb.RemoveComponent<ConnectionState>(netIdEntity);
                    ecb.RemoveComponent<PlayerListEntry>(netIdEntity);
                }
            }
        }

        static void NotifyJoinerOfAllExistingPlayers(ref NativeList<PlayerListEntry> existingPlayers, ref EntityCommandBuffer ecb, in EntityArchetype newJoinerArchetype, in Entity targetConnection)
        {
            for (var i = 0; i < existingPlayers.Length; i++)
            {
                var notifyOthersRpc = ecb.CreateEntity(newJoinerArchetype);
                var changedRpc = existingPlayers[i].State;
                changedRpc.ChangeType = PlayerListEntry.ChangedRpc.UpdateType.ExistingPlayer;
                ecb.SetComponent(notifyOthersRpc, changedRpc);
                ecb.SetComponent(notifyOthersRpc, new SendRpcCommandRequest
                {
                    TargetConnection = targetConnection
                });
            }
        }
    }
}
