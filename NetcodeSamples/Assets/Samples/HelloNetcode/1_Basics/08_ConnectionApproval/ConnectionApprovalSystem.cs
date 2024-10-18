using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;

namespace Samples.HelloNetcode
{
    public struct ClientRequestApproval : IApprovalRpcCommand
    {
        public FixedString4096Bytes Payload;
    }

    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ClientConnectionApprovalSystem : ISystem
    {
        FixedString4096Bytes m_Payload;
        // Mark when we're in approval state but payload isn't ready yet to be sent
        bool m_SendApprovalWhenReady;
        // This is just used to detect if we've fully connected without approval being triggered (so connection approval feature is turned off)
        bool m_ApprovalIsRequired;

        bool AuthenticationIsEnabled(ref SystemState state)
        {
            // Thin clients should always use the dummy payload as they can't use the player authentication service
            if (state.WorldUnmanaged.IsThinClient())
                return false;
            return ConnectionApprovalData.PlayerAuthenticationEnabled.Data;
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Enable use of the player authentication service, when disabled a dummy payload is used
            ConnectionApprovalData.PlayerAuthenticationEnabled.Data = false;

            ConnectionApprovalData.ApprovalPayload.Data = default;
            m_Payload = new FixedString4096Bytes((FixedString4096Bytes)"ABC");
            state.RequireForUpdate<EnableConnectionApproval>();
            state.RequireForUpdate<RpcCollection>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Complain if we've connected without approval, as that's what this sample is demonstrating
            if (!m_ApprovalIsRequired && SystemAPI.HasSingleton<NetworkId>())
            {
                UnityEngine.Debug.LogError($"[{state.WorldUnmanaged.Name}] Connection Approval system ran without connection approval enabled. To test approvals properly you need to load the sample via the Frontend menu as it ensures the feature is enabled.");
                state.Enabled = false;
            }

            // Check connections which have not yet fully connected and send connection approval message
            foreach (var evt in SystemAPI.GetSingleton<NetworkStreamDriver>().ConnectionEventsForTick)
            {
                // Note: It's actually harmless to send your approval even after entering the Handshake state (don't strictly have
                // to wait for Approval state). It's also ok to send this even when connection approval is turned off.
                if (evt.State == ConnectionState.State.Approval)
                    m_ApprovalIsRequired = m_SendApprovalWhenReady = true;
            }

            // If player authentication service is enabled wait until the auth data for the approval payload has been set
            if (AuthenticationIsEnabled(ref state))
            {
                if (ConnectionApprovalData.ApprovalPayload.Data.Payload.Length == 0)
                    return;
                m_Payload = ConnectionApprovalData.ApprovalPayload.Data.Payload;
            }

            // Now we're ready to send the payload, reset the ready state for reconnects (re-use current payload)
            if (m_SendApprovalWhenReady)
            {
                UnityEngine.Debug.Log($"[{state.WorldUnmanaged.Name}] Client sending approval message to server once...");
                ReadOnlySpan<ComponentType> types = stackalloc ComponentType[] {ComponentType.ReadOnly<ClientRequestApproval>(), ComponentType.ReadOnly<SendRpcCommandRequest>()};
                var rpcEntity = state.EntityManager.CreateEntity(types);
                state.EntityManager.SetComponentData(rpcEntity, new ClientRequestApproval {Payload = m_Payload});
                m_SendApprovalWhenReady = false;
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [BurstCompile]
    public partial struct ServerConnectionApprovalSystem : ISystem
    {
        FixedString512Bytes m_DummyPayload;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_DummyPayload = new FixedString512Bytes((FixedString4096Bytes)"ABC");
            ConnectionApprovalData.PendingApprovals.Data = new UnsafeRingQueue<PendingApproval>(32, Allocator.Persistent);
            ConnectionApprovalData.ApprovalResults.Data = new UnsafeRingQueue<ApprovalResult>(32, Allocator.Persistent);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            ConnectionApprovalData.PendingApprovals.Data.Dispose();
            ConnectionApprovalData.ApprovalResults.Data.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Check connections which have not yet fully connected and send connection approval message
            foreach (var (receiveRpc, approvalMsg, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRW<ClientRequestApproval>>().WithEntityAccess())
            {
                // Always clean-up RPC message entity.
                ecb.DestroyEntity(entity);

                var connectionEntity = receiveRpc.ValueRO.SourceConnection;
                var conn = state.EntityManager.GetComponentData<NetworkStreamConnection>(connectionEntity);

                if (state.EntityManager.HasComponent<ConnectionApproved>(connectionEntity))
                {
                    UnityEngine.Debug.LogError($"[{state.WorldUnmanaged.Name}] {conn.Value.ToFixedString()} on {connectionEntity.ToFixedString()} sent approval while already approved!");
                    continue;
                }

                var payload = approvalMsg.ValueRO.Payload;

                // We'll allow the dummy payload if it matches or validate the player if given player id/token
                if (payload.Equals(m_DummyPayload))
                {
                    UnityEngine.Debug.Log($"[{state.WorldUnmanaged.Name}] Approved with dummy payload {conn.Value.ToFixedString()} on {connectionEntity.ToFixedString()}!");
                    // Mark the connection of the message sender as approved
                    ecb.AddComponent<ConnectionApproved>(connectionEntity);
                }
                else
                {
                    var splitIndex = payload.IndexOf(':');
                    FixedString64Bytes playerId = new FixedString64Bytes();
                    playerId.Append(payload.Substring(0, splitIndex));
                    var accessToken = payload.Substring(splitIndex+1, payload.Length);
                    ConnectionApprovalData.PendingApprovals.Data.Enqueue(new PendingApproval(){ PlayerId = playerId, AccessToken = accessToken, Payload = payload, ConnectionEntity = connectionEntity});
                }
            }

            while (ConnectionApprovalData.ApprovalResults.Data.TryDequeue(out var approvalResult))
            {
                var connData = SystemAPI.GetComponentRO<NetworkStreamConnection>(approvalResult.ConnectionEntity);
                if (approvalResult.Success)
                {
                    UnityEngine.Debug.Log($"[{state.WorldUnmanaged.Name}] Approved with player account {connData.ValueRO.Value.ToFixedString()} on {approvalResult.ConnectionEntity.ToFixedString()}!");
                    ecb.AddComponent<ConnectionApproved>(approvalResult.ConnectionEntity);
                }
                else
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD // Don't log this in prod, to avoid leaking real auth tokens, and to prevent malicious users causing log-spam.
                    UnityEngine.Debug.LogError($"[{state.WorldUnmanaged.Name}] Disconnecting {connData.ValueRO.Value.ToFixedString()} on {approvalResult.ConnectionEntity.ToFixedString()} as sent incorrect approval payload '{approvalResult.Payload}'!");
#endif
                    // TODO - Note that this reason is not currently transmitted to the client as part of the close,
                    // but at least the server can query (and log) it.
                    ecb.AddComponent(approvalResult.ConnectionEntity, new NetworkStreamRequestDisconnect
                    {
                        Reason = NetworkStreamDisconnectReason.ApprovalFailure,
                    });
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }

    /// <summary> Data for an incoming approval request from a client, needs to be validated via service </summary>
    public struct PendingApproval
    {
        public FixedString64Bytes PlayerId;
        public FixedString4096Bytes AccessToken;
        public FixedString4096Bytes Payload;    // Store intact payload data for debug purposes
        public Entity ConnectionEntity;
    }

    /// <summary> Data with the result of player account validation </summary>
    public struct ApprovalResult
    {
        public bool Success;
        public FixedString4096Bytes Payload;
        public Entity ConnectionEntity;
    }

    /// <summary>
    /// Communication bridge between DOTS and GameObjects, when data is ready on either side it will be queued and dequeued on the
    /// other side. As there will only ever be one client authenticating a single container for each type of message is enough.
    /// </summary>
    public abstract class ConnectionApprovalData
    {
        public static readonly SharedStatic<bool> PlayerAuthenticationEnabled = SharedStatic<bool>.GetOrCreate<PlayerAuthenticationEnabledKey>();
        /// <summary> Pending and validated approval requests on the server, as there could be multiple clients connecting at the same time these are queued </summary>
        public static readonly SharedStatic<UnsafeRingQueue<PendingApproval>> PendingApprovals = SharedStatic<UnsafeRingQueue<PendingApproval>>.GetOrCreate<PendingApprovalDataKey>();
        public static readonly SharedStatic<UnsafeRingQueue<ApprovalResult>> ApprovalResults = SharedStatic<UnsafeRingQueue<ApprovalResult>>.GetOrCreate<ApprovalResultDataKey>();
        /// <summary> Client payload ready to be sent to server, there should only be one of these as the client will not authenticate multiple times </summary>
        public static readonly SharedStatic<ClientRequestApproval> ApprovalPayload = SharedStatic<ClientRequestApproval>.GetOrCreate<ApprovalPayloadDataKey>();

        // Identifier for the shared static fields
        class PendingApprovalDataKey {}
        class ApprovalResultDataKey {}
        class ApprovalPayloadDataKey {}
        class PlayerAuthenticationEnabledKey {}
    }
}
