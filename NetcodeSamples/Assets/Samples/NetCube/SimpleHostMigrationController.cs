using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Scenes;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Unity.Services.Core;
using UnityEngine.SceneManagement;
using Hash128 = Unity.Entities.Hash128;

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

    /// <summary>
    /// Client and server driver constructors for the host migration scenario. This
    /// will use IPC direct connections between client and server in the same process even
    /// with a simulator enabled (which usually will default to UDP). Clients running
    /// alone will use the relay settings.
    /// </summary>
    public class HostMigrationDriverConstructor : INetworkStreamDriverConstructor
    {
        RelayServerData m_RelayClientData;
        RelayServerData m_RelayServerData;

        public HostMigrationDriverConstructor(RelayServerData serverData, RelayServerData clientData)
        {
            m_RelayServerData = serverData;
            m_RelayClientData = clientData;
        }

        /// <summary>
        /// Connect directly to a local server using IPC or via UDP relay when connecting to remote server.
        /// </summary>
        public void CreateClientDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
            var settings = DefaultDriverBuilder.GetNetworkClientSettings();
            // If this is the local client on the server we'll use IPC otherwise use relay data
            if (ClientServerBootstrap.ServerWorld == null || !ClientServerBootstrap.ServerWorld.IsCreated)
                DefaultDriverBuilder.RegisterClientDriver(world, ref driverStore, netDebug, ref m_RelayClientData);
            else
                DefaultDriverBuilder.RegisterClientIpcDriver(world, ref driverStore, netDebug, settings);
        }

        /// <summary>
        /// Create a server which will listen for IPC connections and connect to the relay data set
        /// here via the constructor.
        /// </summary>
        public void CreateServerDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
            DefaultDriverBuilder.RegisterServerDriver(world, ref driverStore, netDebug, ref m_RelayServerData);
            #else
            throw new NotSupportedException(
                "Creating a server driver for a WebGL build is not supported. You can't listen on a WebSocket in the browser." +
                " WebGL builds should be ideally client-only (has UNITY_CLIENT define) and in case a Client/Server build is made, only client worlds should be created.");
            #endif
        }
    }

    public class SimpleHostMigrationController : MonoBehaviour
    {
        public Lobby CurrentLobby => m_CurrentLobby;
        public string RelayJoinCode { get; set; }

        /// <summary>
        /// Set when the first join of a host is delayed because the host is not yet ready (relay join code not updated)
        /// In this case we'll not react to host migration events until the initial host join is finished.
        /// </summary>
        public bool WaitForInitialJoin { get; set; }

        // By default use the Datagram Transport Layer Security (dtls) connection type with the network transport
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

        public int InitialDataSize { get; set; } = 10_000;

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

        // The time to give a new relay join code to be reported during a host migration event
        const int k_InitialHostJoinWaitTimeoutSeconds = 10;

        string m_LobbyName;
        MigrationDataInfo m_MigrationConfig;
        EntityQuery m_HostMigrationStatsQuery;
        NativeList<byte> m_MigrationDataBlob;
        Task<LobbyUploadMigrationDataResults> m_UploadTask;

        string m_PrevHostId;

        // List of scenes found in the local world before starting client/server worlds
        List<Hash128> m_SceneGuids = new List<Hash128>();

        public string CurrentHostId => m_CurrentLobby == null ? "" : m_CurrentLobby.HostId;
        public bool IsHost => CurrentHostId == CurrentPlayerId;
        public string CurrentLobbyId => m_CurrentLobby == null ? "" : m_CurrentLobby.Id;
        public string CurrentPlayerId => UnityServices.State == ServicesInitializationState.Initialized ?
            AuthenticationService.Instance.PlayerId : "";

#if !UNITY_SERVER
        double m_LastUpdateTime = 0.01;

        void Start()
        {
            // Disable this simple host migration controller as there are already worlds created (frontend controller could be in use)
            if (ClientServerBootstrap.ServerWorld != null || ClientServerBootstrap.ClientWorld != null)
                gameObject.SetActive(false);
            if (ClientServerBootstrap.RequestedPlayType != ClientServerBootstrap.PlayType.ClientAndServer)
                Debug.LogError($"[HostMigration] Creating client/server worlds is not allowed if playmode is set to {ClientServerBootstrap.RequestedPlayType}");
            m_MigrationDataBlob = new NativeList<byte>(InitialDataSize, Allocator.Persistent);
            m_LobbyName = SceneManager.GetActiveScene().name;
        }

        async void OnGUI()
        {
            if (GUILayout.Button("Start Host"))
            {
                await SetupRelayAndLobbyAsHost();
            }
            else if (GUILayout.Button("Connect to host"))
            {
                await JoinLobbyAndConnectWithRelayAsClient();
            }
            else if (GUILayout.Button("Leave"))
            {
                if (ClientServerBootstrap.ServerWorld != null)
                    ClientServerBootstrap.ServerWorld.Dispose();
                if (ClientServerBootstrap.ClientWorld != null)
                    ClientServerBootstrap.ClientWorld.Dispose();
                if (!string.IsNullOrEmpty(CurrentLobbyId) && !string.IsNullOrEmpty(AuthenticationService.Instance.PlayerId))
                    await LobbyService.Instance.RemovePlayerAsync(m_CurrentLobby.Id, AuthenticationService.Instance.PlayerId);
            }
        }

        void OnKickedFromLobby()
        {
            Debug.Log("[HostMigration] Left lobby");
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
            // Just leave the lobby, keep it running as we need it for the other clients for host migration
            if (!string.IsNullOrEmpty(CurrentLobbyId) && !string.IsNullOrEmpty(AuthenticationService.Instance.PlayerId))
                LobbyService.Instance.RemovePlayerAsync(m_CurrentLobby.Id, AuthenticationService.Instance.PlayerId);
        }

        void OnDestroy()
        {
            if (m_Heartbeat != null)
                StopCoroutine(m_Heartbeat);
            if (!string.IsNullOrEmpty(CurrentLobbyId) && !string.IsNullOrEmpty(AuthenticationService.Instance.PlayerId))
                LobbyService.Instance.RemovePlayerAsync(m_CurrentLobby.Id, AuthenticationService.Instance.PlayerId);
        }

        //
        // Host migration handling. Upload host migration data to service at set intervals. React to host migration events
        //

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
                HostMigration.GetHostMigrationData(ClientServerBootstrap.ServerWorld, ref m_MigrationDataBlob);
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

                if (m_MigrationConfig.Expires < DateTime.Now)
                {
                    Debug.Log($"[HostMigration] Refreshing migration config as it has expired.");
                    m_MigrationConfig = await LobbyService.Instance.GetMigrationDataInfoAsync(CurrentLobbyId);
                    Debug.Log($"[HostMigration] Migration Data Information: Expires:{m_MigrationConfig.Expires} (Now:{DateTime.Now}) MaxSize:{m_MigrationConfig.MaxSize} ReadUrl:{m_MigrationConfig.Read} WriteUrl:{m_MigrationConfig.Write}");
                }

                var uploadData = m_MigrationDataBlob.AsArray().ToArray();
                m_UploadTask = LobbyService.Instance.UploadMigrationDataAsync(m_MigrationConfig, uploadData, new LobbyUploadMigrationDataOptions());
                Debug.Log($"[HostMigration][DEBUG] Uploaded migration data, size={uploadData.Length}");
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
                        Debug.LogError($"No client world found during host migration event processing. Aborting.");
                        return;
                    }

                    // Take over heartbeat duties
                    m_Heartbeat = StartCoroutine(HeartbeatLobbyCoroutine());

                    Debug.Log($"[HostMigration] Fetching migration data information");
                    m_MigrationConfig = await LobbyService.Instance.GetMigrationDataInfoAsync(CurrentLobbyId);

                    Debug.Log($"[HostMigration] Migration Data Information: Expires:{m_MigrationConfig.Expires} (Now:{DateTime.Now}) MaxSize:{m_MigrationConfig.MaxSize} ReadUrl:{m_MigrationConfig.Read} WriteUrl:{m_MigrationConfig.Write}");
                    var migrationData = await LobbyService.Instance.DownloadMigrationDataAsync(m_MigrationConfig, new LobbyDownloadMigrationDataOptions());
                    var data = Array.Empty<byte>();
                    if (migrationData != null && migrationData.Data != null)
                        data = migrationData.Data;

                    Debug.Log($"[HostMigration] Received {data.Length} bytes host migration data. Switching self to host role.");
                    var success = await ListenAndConnectWithRelayAsHost(data);
                    if (!success) return;

                    if (ClientServerBootstrap.ServerWorld == null)
                    {
                        Debug.LogError("[HostMigration] Failed to create server world during host migration event. Migration cannot be completed.");
                        return;
                    }

                    // TODO: Wait for cooldown, multiple requests queued, etc

                    await UpdateJoinCodeWhenReady();
                }
                else
                {
                    // Not the host; stop sending lobby heartbeats if we were
                    if (m_Heartbeat != null)
                        StopCoroutine(m_Heartbeat);

                    // In a forced migration, this host is still alive and well but
                    // should stop being a server
                    if (ClientServerBootstrap.ServerWorld != null)
                    {
                        Debug.Log("[HostMigration] Disposing of Server world.");
                        ClientServerBootstrap.ServerWorld.Dispose();
                    }

                    // CheckLobbyDataForNewRelayHost() will be called next to see if the present changes include a new
                    // join code. More likely though, a separate change event will be arriving soon.
                    Debug.Log("[HostMigration] Host migration triggered, waiting for updates from new host before connecting");
                }
            }
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
            using var drvQuery = serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            var networkStreamDriver = drvQuery.GetSingleton<NetworkStreamDriver>();

            Debug.Log("[HostMigration] Waiting for relay connection before join code update");

            // Wait until connection is established
            var relayNetworkDriver = networkStreamDriver.DriverStore.GetDriverRO(2); // On servers by default the first driver is IPC, second is UDP/relay
            var startTime = Time.realtimeSinceStartup;
            serverWorld.EntityManager.CompleteAllTrackedJobs();
            var status = relayNetworkDriver.GetRelayConnectionStatus();
            while (status != RelayConnectionStatus.Established)
            {
                await Task.Delay(100);
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
            HostMigration.ConfigureClientAndConnect(ClientServerBootstrap.ClientWorld, driverConstructor, relayData.Endpoint);
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

            if (!HostMigration.MigrateDataToNewServerWorld(driverConstructor, ref arrayData))
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

        //
        // Initial world creation / bootstrapping, setting up server and client connections, setting up lobby and relay services.
        //

        /// <summary>
        /// Initial service initialization. This only needs to be done once. When running in a standalone player
        /// we need to use a different player profile or else the lobby will treat this player as the same identity
        /// as the editor or other players (would remove everyone from the lobby for example when one player instance
        /// disconnects/leaves). A random profile name will be generated.
        /// </summary>
        async Task InitializeServices()
        {
            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.LinuxPlayer)
            {
                string userprofile = Environment.UserName.Substring(0, math.min(Environment.UserName.Length, 18));
                var random = new System.Random((int)System.Diagnostics.Stopwatch.GetTimestamp());
                for (int i = 0; i < 10; ++i)
                    userprofile += (char)random.Next(65, 90);
                Debug.Log($"[HostMigration] Using random user profile {userprofile}");
                var options = new InitializationOptions();
                options.SetProfile(userprofile);
                await UnityServices.InitializeAsync(options);
            }
            else
            {
                await UnityServices.InitializeAsync();
            }
        }

        /// <summary>
        /// Initial setup for the host of the game session.
        /// </summary>
        async Task SetupRelayAndLobbyAsHost()
        {
            await InitializeServices();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"[HostMigration] Signed in as {AuthenticationService.Instance.PlayerId}");

            var allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers - 1);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"[HostMigration] Created allocation {allocation.AllocationId} with join code {joinCode}");

            SetupRelayHostedServerAndConnect(allocation.ToRelayServerData(ConnectionType));
            Debug.Log($"[HostMigration] Creating lobby with name {m_LobbyName}");
            await CreateLobbyAsync(joinCode, allocation.AllocationId.ToString());
            await SubscribeToLobbyEvents();
        }

        /// <summary>
        /// Initial lobby creation. The same lobby will be kept throughout host migrations so it will only need to be
        /// created once.
        /// </summary>
        public async Task CreateLobbyAsync(string joinCode, string allocationId)
        {
            RelayJoinCode = joinCode;
            var playerId = AuthenticationService.Instance.PlayerId;

            CreateLobbyOptions options = new CreateLobbyOptions();
            options.Data = new Dictionary<string, DataObject>()
            {
                {LobbyKeys.RelayHost, new DataObject(DataObject.VisibilityOptions.Member, playerId)},
                {LobbyKeys.RelayJoinCode, new DataObject(DataObject.VisibilityOptions.Member, joinCode)}
            };
            options.Player = new Player(id: playerId, allocationId: allocationId);

            m_CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(m_LobbyName, k_DefaultMaxLobbyPlayers, options);
            Debug.Log($"[HostMigration] Created lobby {m_CurrentLobby.Id} with name '{m_LobbyName}'");
            m_PrevHostId = CurrentHostId;

            m_MigrationConfig = await LobbyService.Instance.GetMigrationDataInfoAsync(m_CurrentLobby.Id);
            Debug.Log($"[HostMigration] Migration Data Information: Expires:{m_MigrationConfig.Expires} (Now:{DateTime.Now}) MaxSize:{m_MigrationConfig.MaxSize} ReadUrl:{m_MigrationConfig.Read} WriteUrl:{m_MigrationConfig.Write}");

            // Host is responsible for heartbeating the lobby to keep it alive
            m_Heartbeat = StartCoroutine(HeartbeatLobbyCoroutine());
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
        /// Initial setup for clients joining a game session.
        /// </summary>
        async Task JoinLobbyAndConnectWithRelayAsClient()
        {
            await InitializeServices();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"[HostMigration] Signed in as {AuthenticationService.Instance.PlayerId}");

            await JoinLobbyByNameAsync(m_LobbyName);
            await SubscribeToLobbyEvents();

            if (CurrentLobby != null && CurrentLobby.Players != null)
            {
                if (!CurrentLobby.Data.ContainsKey(LobbyKeys.RelayHost) ||
                    !CurrentLobby.Data.ContainsKey(LobbyKeys.RelayJoinCode))
                {
                    Debug.LogError($"[HostMigration] Lobby data missing entries for '{LobbyKeys.RelayHost}' and/or '{LobbyKeys.RelayJoinCode}'");
                    return;
                }

                // If the lobby data has invalid joincode (probably the previous host) then it has not
                // been updated by the current host. A migration is likely taking place and we should wait for
                // the proper join code as normally clients do during a host migration
                if (CurrentLobby.Data[LobbyKeys.RelayHost].Value != CurrentLobby.HostId)
                {
                    Debug.Log("[HostMigration] Relay host does not match lobby host. Will wait for join code");
                    WaitForInitialJoin = true;
                    var timeout = Time.realtimeSinceStartup + k_InitialHostJoinWaitTimeoutSeconds;
                    while (CurrentLobby.Data[LobbyKeys.RelayHost].Value != CurrentLobby.HostId)
                    {
                        if (Time.realtimeSinceStartup > timeout)
                        {
                            Debug.LogError("[HostMigration] Relay host does not match lobby host, timeout while waiting for updated join code.");
                            WaitForInitialJoin = false;
                            return;
                        }
                        await Task.Delay(100);
                    }
                    WaitForInitialJoin = false;
                }
                var relayJoinCode = CurrentLobby.Data[LobbyKeys.RelayJoinCode].Value;
                Debug.Log($"[HostMigration] Using relay join code {relayJoinCode} from lobby data");
                RelayJoinCode = relayJoinCode;
                var allocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
                ConnectToServerWithRelay(allocation.ToRelayServerData(ConnectionType));
                await UpdatePlayerAllocationId(allocation.AllocationId);
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
                    Debug.LogError($"[HostMigration] Failed to join lobby because it is full");
                if (ex.Reason == LobbyExceptionReason.RateLimited)
                    Debug.LogWarning($"[HostMigration] Hit lobby query rate limit while trying to join lobby '{lobbyName}', try again.");
                return;
            }
            Debug.Log($"[HostMigration] Joined lobby ID:{m_CurrentLobby.Id} HostID:{CurrentHostId}");
        }

        /// <summary>
        /// Connect to the relay server and the given server endpoint.
        /// </summary>
        void ConnectToServerWithRelay(RelayServerData relayServerData)
        {
            var oldConstructor = NetworkStreamReceiveSystem.DriverConstructor;
            try
            {
                NetworkStreamReceiveSystem.DriverConstructor = new HostMigrationDriverConstructor(new RelayServerData(), relayServerData);
                var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");

                client.EntityManager.CreateEntity(ComponentType.ReadOnly<EnableHostMigration>());

                if (!TryGetAllScenes())
                    return;

                DestroyLocalWorlds();

                if (World.DefaultGameObjectInjectionWorld == null)
                    World.DefaultGameObjectInjectionWorld = client;

                // Load the previously saved scene into the client world
                foreach (var sceneGuid in m_SceneGuids)
                    SceneSystem.LoadSceneAsync(client.Unmanaged, sceneGuid);

                var networkStreamEntity = client.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestConnect>());
                client.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestConnect");
                client.EntityManager.SetComponentData(networkStreamEntity, new NetworkStreamRequestConnect { Endpoint = relayServerData.Endpoint });
            }
            finally
            {
                NetworkStreamReceiveSystem.DriverConstructor = oldConstructor;
            }
        }

        /// <summary>
        /// Collect relay server end point from completed systems. Set up server with relay support and connect client
        /// to hosted server through relay server.
        /// Both client and server world is manually created to allow us to override the <see cref="DriverConstructor"/>.
        ///
        /// Two singleton entities are constructed with listen and connect requests. These will be executed asynchronously.
        /// Connecting to relay server will not be bound immediately. The Request structs will ensure that we
        /// continuously poll until the connection is established.
        /// </summary>
        void SetupRelayHostedServerAndConnect(RelayServerData relayServerData)
        {
            var oldConstructor = NetworkStreamReceiveSystem.DriverConstructor;
            try
            {
                var driverConstructor = new HostMigrationDriverConstructor(relayServerData, new RelayServerData());
                NetworkStreamReceiveSystem.DriverConstructor = driverConstructor;
                var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
                var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
                NetworkStreamReceiveSystem.DriverConstructor = oldConstructor;

                if (!TryGetAllScenes())
                    return;

                DestroyLocalWorlds();

                if (World.DefaultGameObjectInjectionWorld == null)
                    World.DefaultGameObjectInjectionWorld = server;

                // Load the previously saved scene into the client/server worlds
                Debug.Log("[HostMigration] Loading scenes in client/server worlds");
                foreach (var sceneGuid in m_SceneGuids)
                {
                    SceneSystem.LoadSceneAsync(server.Unmanaged, sceneGuid);
                    SceneSystem.LoadSceneAsync(client.Unmanaged, sceneGuid);
                }

                server.EntityManager.CreateEntity(ComponentType.ReadWrite<EnableHostMigration>());

                using var driverQuery = server.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                var serverDriver = driverQuery.GetSingletonRW<NetworkStreamDriver>();
                if (!serverDriver.ValueRW
                        .Listen(NetworkEndpoint.AnyIpv4))
                {
                    Debug.LogError($"[HostMigration] NetworkStreamDriver.Listen() failed");
                    return;
                }

                var ipcPort = serverDriver.ValueRW.GetLocalEndPoint(serverDriver.ValueRW.DriverStore.FirstDriver).Port;
                var networkStreamEntity = client.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestConnect>());
                client.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestConnect");
                client.EntityManager.SetComponentData(networkStreamEntity, new NetworkStreamRequestConnect { Endpoint = NetworkEndpoint.LoopbackIpv4.WithPort(ipcPort) });
            }
            finally
            {
                NetworkStreamReceiveSystem.DriverConstructor = oldConstructor;
            }
        }

        bool TryGetAllScenes()
        {
            if (m_SceneGuids.Count == 0)
            {
                if (!World.DefaultGameObjectInjectionWorld.IsCreated)
                {
                    Debug.Log("[HostMigration] No local world found. No entity scenes will be loaded in client/server worlds.");
                    return false;
                }
                // Save the current scenes (currently loaded in the local world)
                World.DefaultGameObjectInjectionWorld.EntityManager.GetAllUniqueSharedComponents<SceneSection>(out var sections, Allocator.Temp);
                foreach (var scene in sections)
                {
                    if (scene.SceneGUID.IsValid)
                        m_SceneGuids.Add(scene.SceneGUID);
                }
                if (m_SceneGuids.Count == 0)
                {
                    Debug.LogError("[HostMigration] Failed to find any entity scenes in the local world");
                    return false;
                }
            }
            return true;
        }

        static void DestroyLocalWorlds()
        {
            foreach (var world in World.All)
            {
                if (world.Flags == WorldFlags.Game)
                {
                    world.Dispose();
                    break;
                }
            }
        }
#endif
    }
}
