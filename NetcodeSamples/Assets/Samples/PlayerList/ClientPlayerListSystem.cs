using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace Unity.NetCode.Samples.PlayerList
{
    /// <summary>
    ///     Receives <see cref="PlayerListEntry" /> RPC's, notifying this client of the PRESENCE of other clients.
    /// </summary>
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ClientPlayerListSystem : ISystem, ISystemStartStop
    {
        EntityArchetype m_UsernameRpcArchetype;
        EntityQuery m_InvalidUsernameResponseRpc;
        EntityQuery m_DesiredUsernameChangedQuery;

        public void OnCreate(ref SystemState state)
        {
            OnCreateBurstCompatible(ref state);

            ref var desiredUsernameStore = ref SystemAPI.GetSingletonRW<DesiredUsername>().ValueRW;
            if (desiredUsernameStore.Value.IsEmpty) desiredUsernameStore.Value = UsernameSanitizer.GetDefaultUsername(state.World);

            m_InvalidUsernameResponseRpc = state.GetEntityQuery(ComponentType.ReadOnly<PlayerListEntry.InvalidUsernameResponseRpc>());

            m_DesiredUsernameChangedQuery = state.GetEntityQuery(ComponentType.ReadWrite<DesiredUsername>());
            m_DesiredUsernameChangedQuery.AddChangedVersionFilter(ComponentType.ReadWrite<DesiredUsername>());
        }

        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            var desiredUsername = SystemAPI.GetSingletonRW<DesiredUsername>().ValueRW;
            var netDebug = SystemAPI.GetSingleton<NetDebug>();
            var localPlayerNetworkId = SystemAPI.GetSingleton<NetworkId>().Value;
            var players = SystemAPI.GetSingletonBuffer<PlayerListBufferEntry>();
            ref var entry = ref GetOrCreateEntry(players, localPlayerNetworkId);

            netDebug.DebugLog($"Client {localPlayerNetworkId} has connected, so sending their DesiredUsername '{desiredUsername.Value}' to the server!");
            SendUsernameRpc(ref state, ref desiredUsername, ref entry, "SetUsernameRpc");
        }

        [BurstCompile]
        public void OnStopRunning(ref SystemState state)
        {
            // The implication is that we disconnected.
            SystemAPI.GetSingletonBuffer<PlayerListBufferEntry>().Clear();
        }

        [BurstCompile]
        void OnCreateBurstCompatible(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<DesiredUsername>())
            {
                state.EntityManager.CreateSingleton<DesiredUsername>();
            }

            state.RequireForUpdate<EnablePlayerListsFeature>();
            state.RequireForUpdate<NetworkId>();

            var componentTypes = new NativeArray<ComponentType>(2, Allocator.Temp);
            componentTypes[0] = ComponentType.ReadWrite<PlayerListEntry.ClientRegisterUsernameRpc>();
            componentTypes[1] = ComponentType.ReadWrite<SendRpcCommandRequest>();
            m_UsernameRpcArchetype = state.EntityManager.CreateArchetype(componentTypes);
            componentTypes.Dispose();

            state.EntityManager.CreateSingletonBuffer<PlayerListBufferEntry>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var netDebug = SystemAPI.GetSingleton<NetDebug>();
            var localPlayerNetworkId = SystemAPI.GetSingleton<NetworkId>().Value;
            var players = SystemAPI.GetSingletonBuffer<PlayerListBufferEntry>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            // Handle username RPC:
            if(!m_DesiredUsernameChangedQuery.IsEmpty)
            {
                var desiredUsername = SystemAPI.GetSingletonRW<DesiredUsername>().ValueRW;
                ref var localPlayerEntry = ref GetOrCreateEntry(players, localPlayerNetworkId);
                if (localPlayerEntry.State.Username.Value != desiredUsername.Value)
                {
                    netDebug.DebugLog($"Client {localPlayerNetworkId} has changed their DesiredUsername '{desiredUsername.Value}', so notifying server of change.");
                    SendUsernameRpc(ref state, ref desiredUsername, ref localPlayerEntry, "NotifyUsernameChangedRpc");
                }
            }

            new HandleReceivedStateChangedRpcJob
            {
                ecb = ecb,
                players = players,
                netDebug = netDebug,
                localPlayerNetworkId = localPlayerNetworkId
            }.Schedule();

            // Handle invalid username responses:
            if(!m_InvalidUsernameResponseRpc.IsEmptyIgnoreFilter)
            {
                using var rpcs = m_InvalidUsernameResponseRpc.ToComponentDataArray<PlayerListEntry.InvalidUsernameResponseRpc>(Allocator.Temp);
                foreach (var rpc in rpcs)
                {
                    ref var entry = ref GetOrCreateEntry(players, localPlayerNetworkId);
                    var desiredUsernameStore = SystemAPI.GetSingletonRW<DesiredUsername>().ValueRW;

                    // Note that if the user has already changed their username AGAIN, this invalid response should be ignored (as we've already sent another Username Change Request RPC).
                    if (desiredUsernameStore.Value == rpc.RequestedUsername)
                    {
                        desiredUsernameStore.Value = entry.State.Username.Value;
                        netDebug.LogError($"Local player received InvalidUsernameResponseRpc for '{rpc.RequestedUsername}'. Using '{entry.State.Username.Value}'!");
                    }
                    else netDebug.LogError($"Local player received InvalidUsernameResponseRpc for '{rpc.RequestedUsername}', but user attempting '{desiredUsernameStore.Value}'!");
                }
                state.EntityManager.DestroyEntity(m_InvalidUsernameResponseRpc);
            }
        }

        void SendUsernameRpc(ref SystemState state, ref DesiredUsername desiredUsername, ref PlayerListEntry localPlayerEntry, in FixedString64Bytes rpcName)
        {
            localPlayerEntry.State.Username.Value = desiredUsername.Value = UsernameSanitizer.SanitizeUsername(desiredUsername.Value, localPlayerEntry.State.NetworkId, out _);

            var rpcEntity = state.EntityManager.CreateEntity(m_UsernameRpcArchetype);
            state.EntityManager.SetName(rpcEntity, rpcName);
            state.EntityManager.SetComponentData(rpcEntity, new PlayerListEntry.ClientRegisterUsernameRpc
            {
                Value = desiredUsername.Value
            });
        }

        [BurstCompile]
        [WithAll(typeof(ReceiveRpcCommandRequest))]
        public partial struct HandleReceivedStateChangedRpcJob : IJobEntity
        {
            public EntityCommandBuffer ecb;
            public DynamicBuffer<PlayerListBufferEntry> players;
            public NetDebug netDebug;
            public int localPlayerNetworkId;

            public void Execute(Entity rpcEntity, in PlayerListEntry.ChangedRpc rpc)
            {
                ecb.DestroyEntity(rpcEntity);

                ref var entry = ref GetOrCreateEntry(players, rpc.NetworkId);
                netDebug.DebugLog($"Client {localPlayerNetworkId} received PlayerListEntry.StateChangedRpc: {PlayerListDebugUtils.ToFixedString(entry.State)} >>> {PlayerListDebugUtils.ToFixedString(rpc)}!");
                entry.State = rpc;
            }
        }

        /// <summary>
        ///     Because we store entries in a list, fetching an entry involves:
        ///     1. Ensuring array capacity.
        ///     2. Returning a ref of the entry.
        ///     Note that a default entry is valid.
        /// </summary>
        static unsafe ref PlayerListEntry GetOrCreateEntry(DynamicBuffer<PlayerListBufferEntry> players, int networkId)
        {
            var delta = networkId - players.Length;
            if (delta > 0)
                players.AddRange(new NativeArray<PlayerListBufferEntry>(delta, Allocator.Temp));

            return ref UnsafeUtility.ArrayElementAsRef<PlayerListEntry>(players.GetUnsafePtr(), networkId - 1);
        }
    }
}
