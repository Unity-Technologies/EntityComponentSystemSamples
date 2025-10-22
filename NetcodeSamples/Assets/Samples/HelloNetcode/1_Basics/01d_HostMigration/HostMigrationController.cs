using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.NetCode.HostMigration;
using Unity.NetCode.Samples.Common;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;

namespace Samples.HelloNetcode
{
    /// <summary>
    /// The string keys used in the lobby data to communicate the current joincode and host ID in use by the
    /// games host. During host migration events this will be used so everyone connects to the right relay
    /// allocation.
    /// </summary>
    static class LobbyKeys
    {
        public const string RelayJoinCode = "relay.joinCode";
        public const string RelayHost = "relay.host";
    }

    public class HostMigrationController : MonoBehaviour
    {
        public Lobby CurrentLobby => m_CurrentLobby;
        public string RelayJoinCode { get; set; }

        /// <summary>
        /// Set when the first join of a host is delayed because the host is not yet ready (relay join code not updated)
        /// In this case we'll not react to host migration events until the initial host join is finished.
        /// </summary>
        public bool WaitForInitialJoin { get; set; }

        /// <summary>
        /// By default use the Datagram Transport Layer Security (dtls) connection type with the network transport
        /// </summary>
        public string ConnectionType { get; } = "dtls";

        public int MaxPlayers
        {
            get
            {
                if (m_CurrentLobby != null) return m_CurrentLobby.MaxPlayers;
                return k_DefaultMaxLobbyPlayers;
            }
        }

        Lobby m_CurrentLobby;
        Coroutine m_Heartbeat;
        bool m_HostMigrationEnabled;
        HostMigrationFrontend m_HostMigrationFrontend;

        public int InitialDataSize { get; set; } = 100_000;

        // Interval at which Lobby heartbeats are performed.
        // This must be done at least once every 30s.
        // Note: this call is subject to service rate limiting (max 1 rq/s).
        const int k_HeartbeatIntervalSeconds = 10;

        // Maximum number of players allowed in the created lobby for this game/sample. This is also set in the Lobby
        // cloud config tab for the project ID (see your project on https://cloud.unity.com). Relay's max supported
        // players is: https://docs.unity.com/ugs/manual/relay/manual/limitations
        const int k_DefaultMaxLobbyPlayers = 50;

        // The timeout for establishing a relay connection after a host migration
        const double k_TimeoutForRelayConnectionSeconds = 60;

        // When retrying service API calls use this delay between the calls
        const int k_ServiceRetryDelayMS = 1000;

        // The retry count limit for service API calls
        const int k_ServiceRetryCount = 10;

        MigrationDataInfo m_MigrationConfig;
        EntityQuery m_HostMigrationStatsQuery;
        NativeList<byte> m_MigrationDataBlob;
        Task<LobbyUploadMigrationDataResults> m_UploadTask;

        string m_PrevHostId;

        public string CurrentHostId => m_CurrentLobby == null ? "" : m_CurrentLobby.HostId;
        public bool IsHost => CurrentHostId == CurrentPlayerId;
        public string CurrentLobbyId => m_CurrentLobby == null ? "" : m_CurrentLobby.Id;
        public string CurrentPlayerId => UnityServices.State == ServicesInitializationState.Initialized ?
            AuthenticationService.Instance.PlayerId : "";

        public bool FailNextHostMigration { get; set; }

#if !UNITY_SERVER
        double m_LastUpdateTime = 0.01;

        void Start()
        {
            if (ClientServerBootstrap.AutoConnectPort != 0)
                Debug.LogError("Host migration sample can't run via auto connect (play scene directly) but most be loaded via the frontend");
            if (ClientServerBootstrap.RequestedPlayType != ClientServerBootstrap.PlayType.ClientAndServer)
                Debug.LogError($"[HostMigration] Creating client/server worlds is not allowed if playmode is set to {ClientServerBootstrap.RequestedPlayType}");
            m_MigrationDataBlob = new NativeList<byte>(InitialDataSize, Allocator.Persistent);
            m_HostMigrationFrontend = FindFirstObjectByType<HostMigrationFrontend>();
        }

        internal void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Frontend")
            {
                Debug.Log("[HostMigration] Re-entering frontend scene, leaving lobby");
                m_HostMigrationFrontend = FindFirstObjectByType<HostMigrationFrontend>();
                StopHeartbeat();
                LeaveLobby();
                m_LastUpdateTime = 0;
                m_HostMigrationStatsQuery = default;
                m_HostMigrationEnabled = false;
            }
            // When going from frontend to another scene the feature should be enabled, by now both the client and server worlds will have been created
            else
            {
                var controller = FindFirstObjectByType<HostMigrationController>();
                if (controller != null && !controller.m_HostMigrationEnabled)
                    controller.StartCoroutine(EnableHostMigration());
            }
        }

        void OnKickedFromLobby()
        {
            Debug.Log("[HostMigration] Left lobby");
            if (FindFirstObjectByType<HostMigrationFrontend>() == null)
            {
                Debug.Log($"[HostMigration] Re-entering frontend scene as we've left the lobby now");
                var frontendHud = FindFirstObjectByType<FrontendHUD>();
                frontendHud?.ReturnToFrontend();
            }
        }

        async void OnLobbyChanged(ILobbyChanges changes)
        {
            if (!changes.LobbyDeleted)
            {
                changes.ApplyToLobby(m_CurrentLobby);
                await CheckHostMigration(changes);
                m_PrevHostId = CurrentHostId;
                if (!IsHost)
                    await CheckLobbyDataForNewRelayHost(changes);
            }
        }

        void OnApplicationQuit()
        {
            LeaveLobby();
        }

        void OnDestroy()
        {
            m_MigrationDataBlob.Dispose();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            StopHeartbeat();
            LeaveLobby();
        }

        /// <summary>
        /// Just leave the lobby, keep it running as we need it for the other clients for host migration
        /// </summary>
        void LeaveLobby()
        {
            if (!string.IsNullOrEmpty(CurrentLobbyId) && !string.IsNullOrEmpty(AuthenticationService.Instance.PlayerId))
                LobbyService.Instance.RemovePlayerAsync(m_CurrentLobby.Id, AuthenticationService.Instance.PlayerId);
        }

        void StartHeartbeat()
        {
            StopHeartbeat();
            Debug.Log($"[HostMigration] Starting heartbeat coroutine.");
            m_Heartbeat = StartCoroutine(HeartbeatLobbyCoroutine());
        }

        void StopHeartbeat()
        {
            if (m_Heartbeat != null)
            {
                Debug.Log($"[HostMigration] Stopping heartbeat coroutine.");
                StopCoroutine(m_Heartbeat);
                m_Heartbeat = null;
            }
        }

        public IEnumerator EnableHostMigration()
        {
            if (m_HostMigrationEnabled) yield return null;
            while (!m_HostMigrationEnabled)
            {
                if (ClientServerBootstrap.ServerWorld != null)
                {
                    Debug.Log("[HostMigration] Enable host migration");
                    ClientServerBootstrap.ServerWorld.EntityManager.CreateEntity(ComponentType.ReadOnly<EnableHostMigration>());
                    m_HostMigrationEnabled = true;
                }
                yield return new WaitForSeconds(1f);
            }
        }

        void Update()
        {
            if (ClientServerBootstrap.ServerWorld == null)
                m_HostMigrationStatsQuery = default;

            if (IsHost && ClientServerBootstrap.ServerWorld != null && m_HostMigrationStatsQuery == default)
            {
                var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<HostMigrationStats>();
                var serverWorld = ClientServerBootstrap.ServerWorld;
                m_HostMigrationStatsQuery = builder.Build(serverWorld.EntityManager);
            }

            if (IsHost && ClientServerBootstrap.ServerWorld != null && !string.IsNullOrEmpty(CurrentLobbyId)
                && m_HostMigrationStatsQuery != default && m_HostMigrationStatsQuery.TryGetSingleton<HostMigrationStats>(out var stats) && stats.LastDataUpdateTime > m_LastUpdateTime)
            {
                m_LastUpdateTime = stats.LastDataUpdateTime;
                HostMigrationData.Get(ClientServerBootstrap.ServerWorld, ref m_MigrationDataBlob);
                if (m_MigrationDataBlob.Length > m_MigrationConfig.MaxSize)
                {
                    Debug.LogError($"[HostMigration] Migration data is too large {m_MigrationDataBlob.Length} bytes, maximum is {m_MigrationConfig.MaxSize} bytes. Host migration will not be possible with this data size.");
                    return;
                }

                _ = UploadMigrationData();
            }
        }

        /// <summary>
        /// Upload the host migration data to the host migration service. If needed the upload location URL is
        /// refreshed. This will be called regularly so it's ok if it fails because of service rate limiting.
        /// </summary>
        async Task UploadMigrationData()
        {
            try
            {
                if (m_UploadTask != null && !m_UploadTask.IsCompleted)
                {
                    Debug.Log($"[HostMigration] Previous upload still in progress.");
                    return;
                }

                // Subtract a minute from expiration time to be sure we update the migration URL in time
                if (m_MigrationConfig.Expires.AddMinutes(-1) < DateTime.UtcNow)
                {
                    Debug.Log($"[HostMigration] Refreshing migration config as it has expired.");
                    m_MigrationConfig = await LobbyService.Instance.GetMigrationDataInfoAsync(CurrentLobbyId);
                    Debug.Log($"[HostMigration] Migration Data Information: Expires:{m_MigrationConfig.Expires} (Now:{DateTime.UtcNow}) MaxSize:{m_MigrationConfig.MaxSize} ReadUrl:{m_MigrationConfig.Read} WriteUrl:{m_MigrationConfig.Write}");
                }

                var uploadData = m_MigrationDataBlob.AsArray().ToArray();
                //var startTime = Time.realtimeSinceStartup;
                m_UploadTask = LobbyService.Instance.UploadMigrationDataAsync(m_MigrationConfig, uploadData, new LobbyUploadMigrationDataOptions());
                await m_UploadTask;
                //Debug.Log($"[HostMigration][DEBUG] Uploaded migration data, size={uploadData.Length} time={Time.realtimeSinceStartup - startTime}");
            }
            catch (LobbyServiceException ex)
            {
                if (ex.Reason == LobbyExceptionReason.RateLimited)
                {
                    Debug.LogWarning($"[HostMigration] Hit lobby rate limit while trying to upload migration data, will try again");
                    return;
                }
                Debug.LogError($"[HostMigration] Lobby exception thrown while trying to upload migration data: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        async Task<byte[]> DownloadMigrationDataWithRetry()
        {
            var migrationData = await DownloadMigrationData();
            var retryCount = 0;
            while (migrationData?.Data == null || migrationData.Data.Length == 0)
            {
                if (retryCount++ == k_ServiceRetryCount)
                {
                    Debug.LogError($"[HostMigration] Received 0 bytes migration data after {retryCount} attempts. Failed to download migration data.");
                    break;
                }
                Debug.LogWarning($"[HostMigration] Received 0 bytes migration data. Will retry download (retry count = {retryCount}).");
                await Task.Delay(k_ServiceRetryDelayMS);
                migrationData = await DownloadMigrationData();
            }
            if (migrationData != null && migrationData.Data != null)
                return migrationData.Data;

            return Array.Empty<byte>();
        }

        async Task<LobbyMigrationData> DownloadMigrationData()
        {
            try
            {
                return await LobbyService.Instance.DownloadMigrationDataAsync(m_MigrationConfig, new LobbyDownloadMigrationDataOptions());
            }
            catch (LobbyServiceException ex)
            {
                Debug.Log($"[HostMigration] Failed to download migration data: {ex.Message}");
            }
            return null;
        }

        internal static RelayConnectionStatus GetRelayConnectionStatus(World world)
        {
            using var drvQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            var networkStreamDriver =drvQuery.GetSingleton<NetworkStreamDriver>();

            // Get the server driver with the UDPNetworkInterface and with relay enabled
            RelayConnectionStatus status = RelayConnectionStatus.NotUsingRelay;
            for (var i = networkStreamDriver.DriverStore.FirstDriver;
                 status == RelayConnectionStatus.NotUsingRelay && i < networkStreamDriver.DriverStore.LastDriver;
                 ++i)
            {
                var networkDriver = networkStreamDriver.DriverStore.GetDriverRO(i);
                world.EntityManager.CompleteAllTrackedJobs();
                status = networkDriver.GetRelayConnectionStatus();
            }
            return status;
        }

        /// <summary>
        /// Check the lobby change event for a host migration event.
        ///   - If it's a host migration event for the host we're already connected to it can be ignored
        ///   - Reset the client driver, even if the connection entity is destroyed there can still be a connection
        ///     present to the relay server in the network driver itself.
        ///   - If it's a host migration event for us, we need to take over hosting duties
        ///     - Start the lobby heartbeat (needs to be pinged regularly or the lobby thinks we're inactive)
        ///     - Download the host migration data and deploy it to a new server world (perform host migration)
        ///     - When connected to relay server with a new allocation update the lobby with the new join code
        ///       so the other clients can now connect to me
        ///   - If it's a host migration event, and we'll stay as a client we need to wait until a new join
        ///     code for the new relay allocation has been reported by the new host (so basically do nothing)
        /// </summary>
        async Task CheckHostMigration(ILobbyChanges changes)
        {
            // Do the changes include a hostId modification?
            if (changes.HostId.Changed)
            {
                var newHostId = changes.HostId.Value;
                if (newHostId == m_PrevHostId)
                {
                    Debug.Log($"[HostMigration] Discarding host change event for the current host {m_PrevHostId}.");
                    return;
                }
                Debug.Log($"[HostMigration] Host change event. Previously '{m_PrevHostId}', now '{newHostId}'.");

                // Sever the relay connection of the client right here to ensure it doesn't get in the way later
                // when reconnecting the client
                ResetClientNetworkDriver();

                // Are we the new host?
                if (newHostId == CurrentPlayerId)
                {
                    Debug.Log($"[HostMigration] We are the new elected host!");

                    if (ClientServerBootstrap.ClientWorld == null)
                    {
                        Debug.Log($"No client world found during host migration event processing. Will initialize a new world.");

                        var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
                        m_HostMigrationFrontend.LoadScenes(client);
                    }

                    if (FailNextHostMigration)
                    {
                        Debug.Log("[HostMigration] Manual host migration failure has been triggered. Aborting migration.");
                        return;
                    }

                    // Take over heartbeat duties
                    StartHeartbeat();

                    // It will take a bit of time to download data and do lobby updates, update the UI with what's happening (this will happen in client world as the server world is only created at the end)
                    var clientRelayEntity = SetWaitForRelayConnection(new WaitForRelayConnection() { WaitForHostSetup = true, IsHostMigration = true, StartTime = Time.realtimeSinceStartup});

                    Debug.Log($"[HostMigration] Fetching migration data information");
                    m_MigrationConfig = await LobbyService.Instance.GetMigrationDataInfoAsync(CurrentLobbyId);
                    Debug.Log($"[HostMigration] Migration Data Information: Expires:{m_MigrationConfig.Expires} (Now:{DateTime.Now}) MaxSize:{m_MigrationConfig.MaxSize} ReadUrl:{m_MigrationConfig.Read} WriteUrl:{m_MigrationConfig.Write}");

                    var data = await DownloadMigrationDataWithRetry();
                    if (!ValidateWorldsForMigration()) return;
                    Debug.Log($"[HostMigration] Received {data.Length} bytes host migration data. Switching self to host role.");
                    var success = await ListenAndConnectWithRelayAsHost(data);
                    if (!success)
                    {
                        if (m_CurrentLobby != null && !string.IsNullOrEmpty(CurrentLobbyId))
                            await LobbyService.Instance.RemovePlayerAsync(CurrentLobbyId, AuthenticationService.Instance.PlayerId);
                        return;
                    }

                    if (ClientServerBootstrap.ServerWorld == null)
                    {
                        Debug.LogError("[HostMigration] Failed to create server world during host migration event. Migration cannot be completed.");
                        return;
                    }

                    // TODO: Wait for cooldown, multiple requests queued, etc

                    var waitEntityQuery = ClientServerBootstrap.ServerWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<WaitForRelayConnection>());
                    if (!waitEntityQuery.TryGetSingletonEntity<WaitForRelayConnection>(out var serverRelayEntity))
                        serverRelayEntity = ClientServerBootstrap.ServerWorld.EntityManager.CreateEntity(ComponentType.ReadOnly<WaitForRelayConnection>());
                    waitEntityQuery.Dispose();
                    ClientServerBootstrap.ServerWorld.EntityManager.AddComponentData(serverRelayEntity, new WaitForRelayConnection() { IsHostMigration = true, StartTime = Time.realtimeSinceStartup});
                    ClientServerBootstrap.ClientWorld.EntityManager.DestroyEntity(clientRelayEntity); // cleanup as this is no longer needed

                    // Connect the server migration stats HUD
                    var statsText = FindFirstObjectByType<HostMigrationHUD>().StatsText;
                    ClientServerBootstrap.ServerWorld.GetExistingSystemManaged<ServerHostMigrationHUDSystem>().StatsText = statsText;

                    // Disable the client status HUD
                    ClientServerBootstrap.ClientWorld.GetOrCreateSystemManaged<ClientHostMigrationHUDSystem>().Enabled = false;

                    await UpdateJoinCodeWhenReady();
                }
                else
                {
                    // Not the host; stop sending lobby heartbeats if we were
                    StopHeartbeat();

                    // In a forced migration, this host is still alive and well but
                    // should stop being a server
                    if (ClientServerBootstrap.ServerWorld != null)
                    {
                        Debug.Log("[HostMigration] Disposing of Server world.");
                        ClientServerBootstrap.ServerWorld.Dispose();
                    }

                    // TODO: There should always be a client world at this point, but seems this does happen and needs debugging
                    if (ClientServerBootstrap.ClientWorld != null)
                    {
                        SetWaitForRelayConnection(new WaitForRelayConnection() { WaitForJoinCode = true, OldJoinCode = RelayJoinCode, IsHostMigration = true, StartTime = Time.realtimeSinceStartup});

                        // Connect the migration stats HUD
                        var statsText = FindFirstObjectByType<HostMigrationHUD>().StatsText;
                        var clientMigrationSystem = ClientServerBootstrap.ClientWorld.GetExistingSystemManaged<ClientHostMigrationHUDSystem>();
                        clientMigrationSystem.StatsText = statsText;
                        clientMigrationSystem.Controller = this;
                    }
                    else
                    {
                        Debug.LogWarning("[HostMigration] No client world found during migration event.");
                    }

                    // CheckLobbyDataForNewRelayHost() will be called next to see if the present changes include a new
                    // join code. More likely though, a separate change event will be arriving soon.
                    Debug.Log("[HostMigration] Host migration triggered, waiting for updates from new host before connecting");
                }
            }
        }

        /// <summary>
        /// There could already be a WaitForRelayConnection if we got a host migration event but the host failed and another one was picked
        /// </summary>
        Entity SetWaitForRelayConnection(WaitForRelayConnection waitComponent)
        {
            using var relayEntityQuery = ClientServerBootstrap.ClientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<WaitForRelayConnection>());
            var relayEntity = Entity.Null;
            if (!relayEntityQuery.IsEmptyIgnoreFilter)
                relayEntity = relayEntityQuery.ToEntityArray(Allocator.Temp)[0];
            else
                relayEntity = ClientServerBootstrap.ClientWorld.EntityManager.CreateEntity(ComponentType.ReadOnly<WaitForRelayConnection>());
            ClientServerBootstrap.ClientWorld.EntityManager.AddComponentData(relayEntity, waitComponent);
            return relayEntity;
        }

        /// <summary>
        /// Update the lobby with the new relay allocation id and joincode.
        /// This signals to other players that they can now connect. We need to wait until the connection to the relay
        /// server is established or else the clients might try to join before we're ready to accept incoming
        /// connections (such cases would result in errors on the client side).
        /// </summary>
        async Task UpdateJoinCodeWhenReady()
        {
            var serverWorld = ClientServerBootstrap.ServerWorld;
            var drvQuery = serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            var networkStreamDriver = drvQuery.GetSingletonRW<NetworkStreamDriver>();
            drvQuery.Dispose();

            // Find which driver is using the relay
            int relayDriverNr = 0;
            for (var i = networkStreamDriver.ValueRO.DriverStore.FirstDriver;
                 i < networkStreamDriver.ValueRO.DriverStore.LastDriver;
                 ++i)
            {
                if (networkStreamDriver.ValueRO.DriverStore.GetDriverRO(i).GetRelayConnectionStatus() != RelayConnectionStatus.NotUsingRelay)
                {
                    relayDriverNr = i;
                    break;
                }
            }
            Debug.Log("[HostMigration] Waiting for relay connection before join code update");

            // Wait until connection is established
            var relayNetworkDriver = networkStreamDriver.ValueRO.DriverStore.GetDriverRO(relayDriverNr);
            var startTime = Time.realtimeSinceStartup;
            serverWorld.EntityManager.CompleteAllTrackedJobs();
            var status = relayNetworkDriver.GetRelayConnectionStatus();
            while (status != RelayConnectionStatus.Established)
            {
                await Task.Delay(100);
                // Server world has been destroyed while waiting to update the join code (most likely returned to main menu)
                if (serverWorld == null || !serverWorld.IsCreated)
                {
                    // Leave immediately as we're the host and someone else needs to take over
                    LeaveLobby();
                    return;
                }
                serverWorld.EntityManager.CompleteAllTrackedJobs();
                status = relayNetworkDriver.GetRelayConnectionStatus();
                if (Time.realtimeSinceStartup - startTime > k_TimeoutForRelayConnectionSeconds)
                {
                    Debug.LogError($"[HostMigration] Timeout while waiting for relay connection before announcing join code ({status})");
                    return;
                }
            }

            var connectionTime = Time.realtimeSinceStartup - startTime;
            Debug.Log($"[HostMigration] Relay connection established ({connectionTime:F2} s). Updating relay join code.");

            // Announce the join code as relay should be ready for the clients now
            UpdateLobbyOptions updateLobbyOptions = new UpdateLobbyOptions()
            {
                Data = new Dictionary<string, DataObject>(){
                    {
                        LobbyKeys.RelayHost,
                        new DataObject(DataObject.VisibilityOptions.Member, CurrentPlayerId)
                    },
                    {
                        LobbyKeys.RelayJoinCode,
                        new DataObject(DataObject.VisibilityOptions.Member, RelayJoinCode)
                    },
                },
            };
            m_CurrentLobby = await LobbyService.Instance.UpdateLobbyAsync(m_CurrentLobby.Id, updateLobbyOptions);
            Debug.Log($"[HostMigration] Updated relay join code in lobby");
        }

        /// <summary>
        /// The relay connection isn't immediately removed when the host is disconnected, this can be forced by
        /// disposing the NetworkDriver (via resetting the driver store).
        /// </summary>
        void ResetClientNetworkDriver()
        {
            var client = ClientServerBootstrap.ClientWorld;
            if (client == null) return;
            using var clientNetDebugQuery = client.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetDebug>());
            var clientNetDebug = clientNetDebugQuery.GetSingleton<NetDebug>();
            var clientDriverStore = new NetworkDriverStore();
            DefaultDriverBuilder.DefaultDriverConstructor.CreateClientDriver(client, ref clientDriverStore, clientNetDebug);
            using var clientDriverQuery = client.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            var clientDriver = clientDriverQuery.GetSingleton<NetworkStreamDriver>();
            clientDriver.ResetDriverStore(client.Unmanaged, ref clientDriverStore);
        }

        /// <summary>
        /// Check if the new host has updated the relay connection info on the lobby data.
        /// If a new and valid join code is being reported we'll connect to the new host using the relay.
        /// </summary>
        async Task CheckLobbyDataForNewRelayHost(ILobbyChanges changes)
        {
            if (!changes.Data.Changed)
                return;

            Debug.Log($"[HostMigration] Data change event received.");
            var lobbyData = changes.Data.Value;
            if (lobbyData.ContainsKey(LobbyKeys.RelayJoinCode))
            {
                if (WaitForInitialJoin)
                {
                    Debug.Log($"[HostMigration] Ignoring host migration event as we've been waiting for the join code of the initial host.");
                    return;
                }

                if (m_PrevHostId == changes.HostId.Value)
                {
                    Debug.Log($"[HostMigration] Discarding host change event for the current host {m_PrevHostId}.");
                    return;
                }

                string relayHost;
                if (lobbyData.ContainsKey(LobbyKeys.RelayHost) && !lobbyData[LobbyKeys.RelayJoinCode].Removed)
                {
                    relayHost = lobbyData[LobbyKeys.RelayHost].Value.Value;
                }
                else if (m_CurrentLobby.Data.ContainsKey(LobbyKeys.RelayHost))
                {
                    relayHost = m_CurrentLobby.Data[LobbyKeys.RelayHost].Value;
                }
                else
                {
                    Debug.LogWarning($"[HostMigration] Ignoring unattributed relay join code; current host is {CurrentHostId}.");
                    return;
                }

                // Upon host migration, the lobby updates immediately before the new lobby host has updated
                // the relay join code. In this update, the (now stale) relay information from the previous lobby
                // host needs to be ignored.
                if (relayHost != CurrentHostId)
                {
                    Debug.Log($"[HostMigration] Ignoring stale relay join code from host {relayHost}; current host is {CurrentHostId}.");
                    return;
                }

                var newRelayJoinCode = lobbyData[LobbyKeys.RelayJoinCode].Value.Value;
                if (newRelayJoinCode == RelayJoinCode)
                {
                    Debug.Log($"[HostMigration] Discarding join code event for client (already using this join code '{RelayJoinCode}').");
                    return;
                }

                Debug.Log($"[HostMigration] New relay join code received {newRelayJoinCode}");
                await ConnectWithRelayAsClient(newRelayJoinCode);
            }
        }

        /// <summary>
        /// Sanity check on the current world status before a client becomes as host during
        /// a host migration event.
        /// - Can't already have a server world as we'll be creating a new one
        /// - Thin clients are not supported
        /// - Client world must exist, it will switch from relay connection setup (old host)
        ///   to an IPC to the local server world
        /// </summary>
        static bool ValidateWorldsForMigration()
        {
            World clientWorld = default;
            foreach (var world in World.All)
            {
                if (world.IsServer())
                {
                    Debug.LogError("Server already present during host migration start. Aborting host migration.");
                    return false;
                }
                if (world.IsThinClient())
                {
                    Debug.LogError("Cannot migrate thin client to server! Aborting host migration.");
                    return false;
                }
                if (world.IsClient())
                {
                    if (clientWorld != default)
                    {
                        Debug.LogError("More than one client world present, this is not allowed. Aborting host migration.");
                        return false;
                    }
                    clientWorld = world;
                }
            }
            if (clientWorld == default)
            {
                Debug.LogError("No client world found during host migration. Aborting host migration.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// The host migration routine for clients who remain as clients.
        ///   - No new client world is created but the current one kept intact
        ///   - Reset the client driver store with the new relay allocation/joincode
        ///   - Connect to the new host
        /// </summary>
        async Task ConnectWithRelayAsClient(string joinCode)
        {
            RelayJoinCode = joinCode;
            var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            await UpdatePlayerAllocationId(allocation.AllocationId);
            var relayData = allocation.ToRelayServerData(ConnectionType);
            var driverConstructor = new HostMigrationDriverConstructor(new RelayServerData(), relayData);
            HostMigrationHelper.ConfigureClientAndConnect(ClientServerBootstrap.ClientWorld, driverConstructor, relayData.Endpoint);
        }

        /// <summary>
        /// The host migration routine for the new host.
        ///   - Create a new relay allocation and join code
        ///   - Create a new server world and deploy the migration data there.
        /// </summary>
        async Task<bool> ListenAndConnectWithRelayAsHost(byte[] migrationData)
        {
            if (migrationData.Length == 0)
            {
                Debug.LogError($"No migration data given during host migration event.");
                return false;
            }

            var allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers - 1);
            Debug.Log($"[HostMigration] Created new relay allocation {allocation.AllocationId}");
            await UpdatePlayerAllocationId(allocation.AllocationId);

            var hostRelayData = allocation.ToRelayServerData(ConnectionType);
            var driverConstructor = new HostMigrationDriverConstructor(hostRelayData, new RelayServerData());

            m_MigrationDataBlob.ResizeUninitialized(migrationData.Length);
            var arrayData = m_MigrationDataBlob.AsArray();
            var slice = new NativeSlice<byte>(arrayData, 0, migrationData.Length);
            slice.CopyFrom(migrationData);

            if (!HostMigrationHelper.MigrateDataToNewServerWorld(driverConstructor, ref arrayData))
            {
                Debug.LogError($"[HostMigration] HostMigration.MigrateDataToNewServerWorld failed. Aborting host migration.");
                return false;
            }

            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"[HostMigration] Obtained joincode {joinCode} for allocation {allocation.AllocationId}");
            RelayJoinCode = joinCode;
            return true;
        }

        /// <summary>
        /// Update the relay allocation ID for the local player in the lobby. This needs to be done every time the
        /// allocation changes so the lobby and relay can identify this endpoint reliably (for example if the player
        /// times out in the relay the lobby will know as well).
        /// </summary>
        /// <param name="allocationId"></param>
        public async Task UpdatePlayerAllocationId(Guid allocationId)
        {
            var updatePlayerOptions = new UpdatePlayerOptions() { AllocationId = allocationId.ToString() };
            m_CurrentLobby = await LobbyService.Instance.UpdatePlayerAsync(CurrentLobbyId, CurrentPlayerId, updatePlayerOptions);
            await LobbyService.Instance.ReconnectToLobbyAsync(CurrentLobbyId);
        }

        /// <summary>
        /// Send a heartbeat to the current lobby at intervals specified by <see cref="k_HeartbeatIntervalSeconds"/>.
        /// Only the server needs to send the heartbeat (keeps the lobby alive).
        /// </summary>
        /// <returns></returns>
        IEnumerator HeartbeatLobbyCoroutine()
        {
            while (true)
            {
                if (m_CurrentLobby == null)
                    yield return null;

                try
                {
                    LobbyService.Instance.SendHeartbeatPingAsync(CurrentLobbyId);
                }
                catch (LobbyServiceException ex)
                {
                    if (ex.Reason == LobbyExceptionReason.RateLimited)
                        Debug.LogWarning($"[HostMigration] Hit lobby heartbeat rate limit, will try again in {k_HeartbeatIntervalSeconds} seconds.");
                    else
                        Debug.LogError($"[HostMigration] Lobby exception while sending heartbeat: {ex.Message}.");
                }
                yield return new WaitForSecondsRealtime(k_HeartbeatIntervalSeconds);
            }
        }

        /// <summary>
        /// Initial lobby creation. The same lobby will be kept throughout host migrations so it will only need to be
        /// created once.
        /// </summary>
        public async Task CreateLobbyAsync(string joinCode, string allocationId)
        {
            // TODO: Handle potentially hitting rate limit, wait until this succeeds

            RelayJoinCode = joinCode;
            var playerId = AuthenticationService.Instance.PlayerId;

            CreateLobbyOptions options = new CreateLobbyOptions();
            options.Data = new Dictionary<string, DataObject>()
            {
                {LobbyKeys.RelayHost, new DataObject(DataObject.VisibilityOptions.Member, playerId)},
                {LobbyKeys.RelayJoinCode, new DataObject(DataObject.VisibilityOptions.Member, joinCode)}
            };
            options.Player = new Player(id: playerId, allocationId: allocationId);

            m_CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(m_HostMigrationFrontend.Address.text, k_DefaultMaxLobbyPlayers, options);
            Debug.Log($"[HostMigration] Created lobby {m_CurrentLobby.Id} with name '{m_HostMigrationFrontend.Address.text}'");
            m_PrevHostId = CurrentHostId;

            m_MigrationConfig = await LobbyService.Instance.GetMigrationDataInfoAsync(m_CurrentLobby.Id);
            Debug.Log($"[HostMigration] Migration Data Information: Expires:{m_MigrationConfig.Expires} (Now:{DateTime.Now}) MaxSize:{m_MigrationConfig.MaxSize} ReadUrl:{m_MigrationConfig.Read} WriteUrl:{m_MigrationConfig.Write}");

            // Host is responsible for heartbeating the lobby to keep it alive
            StartHeartbeat();
        }

        /// <summary>
        /// Initial subscription to lobby events. We need this to get notifications about host migration events. This
        /// only needs to happen once as the lobby is kept intact throughout host migrations.
        /// </summary>
        public async Task SubscribeToLobbyEvents()
        {
            if (string.IsNullOrEmpty(CurrentLobbyId))
                return;
            var callbacks = new LobbyEventCallbacks();
            callbacks.LobbyChanged += OnLobbyChanged;
            callbacks.KickedFromLobby += OnKickedFromLobby;
            try {
                await LobbyService.Instance.SubscribeToLobbyEventsAsync(m_CurrentLobby.Id, callbacks);
                Debug.Log($"[HostMigration] Subscribed to lobby events lobbyId:{m_CurrentLobby.Id}");
            }
            catch (LobbyServiceException ex)
            {
                switch (ex.Reason) {
                    case LobbyExceptionReason.AlreadySubscribedToLobby: Debug.LogWarning($"Already subscribed to lobby[{m_CurrentLobby.Id}]. We did not need to try and subscribe again. Exception Message: {ex.Message}"); break;
                    case LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy: Debug.LogError($"Subscription to lobby events was lost while it was busy trying to subscribe. Exception Message: {ex.Message}"); throw;
                    case LobbyExceptionReason.LobbyEventServiceConnectionError: Debug.LogError($"Failed to connect to lobby events. Exception Message: {ex.Message}"); throw;
                    default: throw;
                }
            }
        }

        /// <summary>
        /// Initial join handling for a specific lobby name. The clients will only need to join the lobby once during
        /// a game session as the host migration will keep using the same lobby. This will fail if the exact lobby
        /// name is not found.
        /// </summary>
        public async Task JoinLobbyByNameAsync(string lobbyName)
        {
            var queryLobbiesOptions = new QueryLobbiesOptions();
            queryLobbiesOptions.Filters = new List<QueryFilter>() { new QueryFilter(QueryFilter.FieldOptions.Name, lobbyName, QueryFilter.OpOptions.EQ) };
            try
            {
                QueryResponse lobbies = await LobbyService.Instance.QueryLobbiesAsync();
                if (lobbies.Results.Count == 0)
                {
                    Debug.LogError($"[HostMigration] Lobby not found: '{lobbyName}'");
                    return;
                }

                var foundLobby = lobbies.Results.FirstOrDefault(x => x.Name.Equals(lobbyName));
                if (foundLobby != null)
                {
                    Debug.Log($"[HostMigration] Joining lobby name:{lobbyName} id:{foundLobby.Id} HostId:{foundLobby.HostId}");
                }
                else
                {
                    Debug.LogError($"[HostMigration] Lobby not found: '{lobbyName}'.");
                    m_HostMigrationFrontend.ClientConnectionStatus = $"Lobby '{lobbyName}' not found. Found {lobbies.Results.Count} other lobbies.";
                    foreach (var lobby in lobbies.Results)
                        Debug.LogWarning($"[HostMigration] Name:{lobby.Name} ID:{lobby.Id} HostID:{lobby.HostId}");
                    return;
                }

                // Host ID needs to be set before joining as during join operation you could get a host migration event for this host
                // (owner of lobby) but we're already connecting to him so this event needs to be ignored
                m_PrevHostId = foundLobby.HostId;
                m_CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(foundLobby.Id);
            }
            catch (LobbyServiceException ex)
            {
                if (ex.Reason == LobbyExceptionReason.LobbyFull)
                {
                    Debug.LogError($"[HostMigration] Failed to join lobby because it is full");
                    m_HostMigrationFrontend.ClientConnectionStatus = "Lobby is full!";
                }
                else if (ex.Reason == LobbyExceptionReason.RateLimited)
                {
                    Debug.LogWarning($"[HostMigration] Hit lobby query rate limit while trying to join lobby '{lobbyName}', try again.");
                    return;
                }
                // TODO: This is mostly debugging info in case this exception is unexpectedly hit (remove later)
                if (m_CurrentLobby == null)
                    Debug.Log("DEBUG: No lobby instance found.");
                var joinedLobbies = await LobbyService.Instance.GetJoinedLobbiesAsync();
                if (joinedLobbies.Count > 0)
                {
                    foreach (var lobby in joinedLobbies)
                        Debug.Log($"DEBUG: Already joined lobby: {lobby}");
                    Debug.Log($"DEBUG: Getting first lobby: {joinedLobbies[0]}");
                    m_CurrentLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobbies[0]);
                }
                else
                {
                    return;
                }
            }
            Debug.Log($"[HostMigration] Joined lobby ID:{m_CurrentLobby.Id} HostID:{CurrentHostId}");
        }
#endif
    }
}
