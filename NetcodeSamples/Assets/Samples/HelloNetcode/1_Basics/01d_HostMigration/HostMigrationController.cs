using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
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
    static class LobbyKeys
    {
        public const string RelayJoinCode = "relay.joinCode";
        public const string RelayHost = "relay.host";
    }

    public class HostMigrationController : MonoBehaviour
    {
        public Lobby CurrentLobby => m_CurrentLobby;
        public string RelayJoinCode { get; set; }

        // Set when the the first join of a host is delayed because the host is not yet ready (relay join code not updated)
        // In this case we'll not react to host migration events until the initial host join is finished.
        public bool WaitForInitialJoin { get; set; }

        // By default use the Datagram Transport Layer Security (dtls) connection type with the network transport
        public string ConnectionType { get; } = "udp";

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

        // Interval at which Lobby heartbeats are performed.
        // This must be done at least once every 30s.
        // Note: this call is subject to service rate limiting (max 1 rq/s).
        const int k_HeartbeatIntervalSeconds = 10;

        // Maximum number of players allowed in the created lobby
        const int k_DefaultMaxLobbyPlayers = 50;

        // The timeout for establishing a relay connection after a host migration
        const double k_TimeoutForRelayConnection = 60;

        MigrationDataInfo m_MigrationConfig;
        EntityQuery m_HostMigrationStatsQuery;
        NativeList<byte> m_MigrationDataBlob;
        float m_UpdatePauseTimer;
        bool m_UploadInProgress;

        string m_PrevHostId;

        public string CurrentHostId => m_CurrentLobby == null ? "" : m_CurrentLobby.HostId;
        public bool IsHost => CurrentHostId == CurrentPlayerId;
        public string CurrentLobbyId => m_CurrentLobby == null ? "" : m_CurrentLobby.Id;
        public string CurrentPlayerId => UnityServices.State == ServicesInitializationState.Initialized ?
            AuthenticationService.Instance.PlayerId : "";

#if !UNITY_SERVER
        double m_LastUpdateTime = 0.01;

        void Start()
        {
            if (ClientServerBootstrap.AutoConnectPort != 0)
                Debug.LogError("Host migration sample can't run via auto connect (play scene directly) but most be loaded via the frontend");
            m_MigrationDataBlob = new NativeList<byte>(10_000, Allocator.Persistent);
            m_HostMigrationFrontend = FindFirstObjectByType<HostMigrationFrontend>();
        }

        internal void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Frontend")
            {
                Debug.Log($"[HostMigration] Re-entering frontend scene, leaving lobby");
                m_HostMigrationFrontend = FindFirstObjectByType<HostMigrationFrontend>();
                if (m_Heartbeat != null)
                {
                    var controller = FindFirstObjectByType<HostMigrationController>();
                    if (controller != null && !controller.m_HostMigrationEnabled)
                        controller.StopCoroutine(m_Heartbeat);
                }
                if (m_CurrentLobby != null && !string.IsNullOrEmpty(CurrentLobbyId))
                    LobbyService.Instance.RemovePlayerAsync(CurrentLobbyId, AuthenticationService.Instance.PlayerId);
                m_HostMigrationStatsQuery = default;
            }
            // When going from frontend to another scene the feature should be enabled, by now both the client and server worlds will have been created
            else
            {
                var controller = FindFirstObjectByType<HostMigrationController>();
                if (controller != null && !controller.m_HostMigrationEnabled)
                    controller.StartCoroutine(EnableHostMigration());
            }
        }

        public IEnumerator EnableHostMigration()
        {
            if (m_HostMigrationEnabled) yield return null;
            while (!m_HostMigrationEnabled)
            {
                if (ClientServerBootstrap.ServerWorld != null)
                {
                    ClientServerBootstrap.ServerWorld.EntityManager.CreateEntity(ComponentType.ReadOnly<EnableHostMigration>());
                    m_HostMigrationEnabled = true;
                }
                foreach (var world in ClientServerBootstrap.ClientWorlds)
                {
                    world.EntityManager.CreateEntity(ComponentType.ReadOnly<EnableHostMigration>());
                    m_HostMigrationEnabled = true;
                }
                yield return new WaitForSeconds(1f);
            }
        }

        void Update()
        {
            if (IsHost && ClientServerBootstrap.ServerWorld != null && m_HostMigrationStatsQuery == default)
            {
                var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<HostMigrationStats>();
                var serverWorld = ClientServerBootstrap.ServerWorld;
                m_HostMigrationStatsQuery = builder.Build(serverWorld.EntityManager);
            }

            if (m_UpdatePauseTimer < Time.realtimeSinceStartup && IsHost && ClientServerBootstrap.ServerWorld != null && m_CurrentLobby != null && !string.IsNullOrEmpty(m_CurrentLobby.Id)
                && m_HostMigrationStatsQuery != default && m_HostMigrationStatsQuery.TryGetSingleton<HostMigrationStats>(out var stats) && stats.LastDataUpdateTime > m_LastUpdateTime)
            {
                m_LastUpdateTime = stats.LastDataUpdateTime;
                m_MigrationDataBlob.Length = 0;
                var arrayData = m_MigrationDataBlob.AsArray();
                HostMigration.TryGetHostMigrationData(ref arrayData, out var migrationDataSize);
                m_MigrationDataBlob.ResizeUninitialized(migrationDataSize);
                arrayData = m_MigrationDataBlob.AsArray();
                if (!HostMigration.TryGetHostMigrationData(ref arrayData, out migrationDataSize))
                {
                    Debug.LogError($"Migration data doesn't fit into given buffer (required size = {migrationDataSize}, destination buffer = {arrayData.Length})");
                    return;
                }
                if (migrationDataSize > m_MigrationConfig.MaxSize)
                {
                    Debug.LogError($"Migration data is too large {migrationDataSize} bytes, maximum is {m_MigrationConfig.MaxSize} bytes");
                    return;
                }

                _ = UploadMigrationData(migrationDataSize);
            }
        }

        async Task UploadMigrationData(int migrationDataSize)
        {
            try
            {
                if (m_UploadInProgress)
                {
                    Debug.Log($"[HostMigration] Previous upload still in progress.");
                    return;
                }
                m_UploadInProgress = true;

                if (m_MigrationConfig.Expires < DateTime.Now)
                {
                    Debug.Log($"[HostMigration] Refreshing migration config as it has expired.");
                    m_MigrationConfig = await LobbyService.Instance.GetMigrationDataInfoAsync(CurrentLobbyId);
                    Debug.Log($"[HostMigration] Migration Data Information: Expires:{m_MigrationConfig.Expires} (Now:{DateTime.Now}) MaxSize:{m_MigrationConfig.MaxSize} ReadUrl:{m_MigrationConfig.Read} WriteUrl:{m_MigrationConfig.Write}");
                }

                var uploadData = m_MigrationDataBlob.AsArray().ToArray();
                // var startTime = Time.realtimeSinceStartup;
                await LobbyService.Instance.UploadMigrationDataAsync(m_MigrationConfig, uploadData, new LobbyUploadMigrationDataOptions());
                // Debug.Log($"[DEBUG] Uploaded migration data, size={migrationDataSize} array={uploadData.Length} time={Time.realtimeSinceStartup - startTime}");
                m_UploadInProgress = false;
            }
            catch (LobbyServiceException ex)
            {
                m_UploadInProgress = false;
                if (ex.Reason == LobbyExceptionReason.RateLimited)
                {
                    Debug.LogWarning($"Hit lobby rate limit while trying to upload migration data, will try again");
                    return;
                }
                Debug.LogError($"Lobby exception: {ex.Message}");
            }
        }

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
            m_Heartbeat = StartCoroutine(HeartbeatLobbyCoroutine());
        }

        public async Task UpdatePlayerAllocationId(Guid allocationId)
        {
            var updatePlayerOptions = new UpdatePlayerOptions() { AllocationId = allocationId.ToString() };
            m_CurrentLobby = await LobbyService.Instance.UpdatePlayerAsync(CurrentLobbyId, CurrentPlayerId, updatePlayerOptions);
            await LobbyService.Instance.ReconnectToLobbyAsync(CurrentLobbyId);
        }

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

        public async Task SubscribeToLobbyEvents()
        {
            if (m_CurrentLobby == null)
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

                    // Take over heartbeat duties
                    m_Heartbeat = StartCoroutine(HeartbeatLobbyCoroutine());

                    // Add a delay before starting the host data update loop
                    m_UpdatePauseTimer = Time.realtimeSinceStartup + 5.0f;

                    if (ClientServerBootstrap.ClientWorld == null)
                    {
                        Debug.LogError($"No client world found during host migration event processing. Aborting.");
                        return;
                    }

                    // It will take a bit of time to download data and do lobby updates, update the UI with what's happening (this will happen in client world as the server world is only created at the end)
                    var clientRelayEntity = ClientServerBootstrap.ClientWorld.EntityManager.CreateEntity(ComponentType.ReadOnly<WaitForRelayConnection>());
                    ClientServerBootstrap.ClientWorld.EntityManager.AddComponentData(clientRelayEntity, new WaitForRelayConnection() { WaitForHostSetup = true, IsHostMigration = true, StartTime = Time.realtimeSinceStartup});

                    Debug.Log($"[HostMigration] Fetching migration data information");
                    m_MigrationConfig = await LobbyService.Instance.GetMigrationDataInfoAsync(CurrentLobbyId);

                    Debug.Log($"[HostMigration] Migration Data Information: Expires:{m_MigrationConfig.Expires} (Now:{DateTime.Now}) MaxSize:{m_MigrationConfig.MaxSize} ReadUrl:{m_MigrationConfig.Read} WriteUrl:{m_MigrationConfig.Write}");
                    var migrationData = await LobbyService.Instance.DownloadMigrationDataAsync(m_MigrationConfig, new LobbyDownloadMigrationDataOptions());
                    var data = Array.Empty<byte>();
                    if (migrationData != null && migrationData.Data != null)
                        data = migrationData.Data;
                    Debug.Log($"[HostMigration] Received {data.Length} bytes");

                    if (!ValidateWorldsForMigration()) return;
                    Debug.Log("[HostMigration] Switching self to host role after migration");
                    var success = await ListenAndConnectWithRelayAsHost(data);
                    if (!success || ClientServerBootstrap.ServerWorld == null)
                    {
                        Debug.LogError($"Fatal error during host migration :(");
                        return;
                    }

                    // TODO: Wait for cooldown, multiple requests queued, etc

                    using var waitEntityQuery = ClientServerBootstrap.ServerWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<WaitForRelayConnection>());
                    if (!waitEntityQuery.TryGetSingletonEntity<WaitForRelayConnection>(out var serverRelayEntity))
                        serverRelayEntity = ClientServerBootstrap.ServerWorld.EntityManager.CreateEntity(ComponentType.ReadOnly<WaitForRelayConnection>());
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
                    if (m_Heartbeat != null)
                        StopCoroutine(m_Heartbeat);

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
                        Debug.LogWarning("[HostMigration] No client world found during migration event. Creating a new world.");
                        var relayEntity = ClientServerBootstrap.ClientWorld.EntityManager.CreateEntity(ComponentType.ReadOnly<WaitForRelayConnection>());
                        ClientServerBootstrap.ClientWorld.EntityManager.AddComponentData(relayEntity, new WaitForRelayConnection() { WaitForJoinCode = true, OldJoinCode = RelayJoinCode, IsHostMigration = true, StartTime = Time.realtimeSinceStartup});

                        // Connect the migration stats HUD
                        var statsText = FindFirstObjectByType<HostMigrationHUD>().StatsText;
                        var clientMigrationSystem = ClientServerBootstrap.ClientWorld.GetExistingSystemManaged<ClientHostMigrationHUDSystem>();
                        clientMigrationSystem.StatsText = statsText;
                        clientMigrationSystem.Controller = this;
                    }

                    // CheckLobbyDataForNewRelayHost() will be called next to see if the present changes include a new
                    // join code. More likely though, a separate change event will be arriving soon.
                    Debug.Log("[HostMigration] Host migration triggered, waiting for updates from new host before connecting");
                }
            }
        }

        /// <summary>
        /// Update the lobby with the new relay allocation id and joincode
        /// This signals to other players that they can now connect
        /// </summary>
        async Task UpdateJoinCodeWhenReady()
        {
            var serverWorld = ClientServerBootstrap.ServerWorld;
            using var drvQuery = serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            var networkStreamDriver = drvQuery.GetSingletonRW<NetworkStreamDriver>();

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
                serverWorld.EntityManager.CompleteAllTrackedJobs();
                status = relayNetworkDriver.GetRelayConnectionStatus();
                if (Time.realtimeSinceStartup - startTime > k_TimeoutForRelayConnection)
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
        /// The relay connection isn't immediately removed when the host is disconnected, this can be forced by disposing the NetworkDriver
        /// (via resetting the driver store). This will be done again when we have a new relay allocation to pass to the driver contructor later
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
        /// Check if the new host has updated the relay connection info on the lobby data
        /// </summary>
        async Task CheckLobbyDataForNewRelayHost(ILobbyChanges changes)
        {
            if (changes.Data.Changed)
            {
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
        }

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

        async Task ConnectWithRelayAsClient(string joinCode)
        {
            RelayJoinCode = joinCode;
            var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            await UpdatePlayerAllocationId(allocation.AllocationId);
            var relayData = allocation.ToRelayServerData(ConnectionType);
            var driverConstructor = new HostMigrationDriverConstructor(new RelayServerData(), relayData);
            HostMigration.ConfigureClientAndConnect(ClientServerBootstrap.ClientWorld, driverConstructor, relayData.Endpoint);
        }

        IEnumerator HeartbeatLobbyCoroutine()
        {
            while (true)
            {
                if (m_CurrentLobby == null)
                    yield return null;

                try
                {
                    LobbyService.Instance.SendHeartbeatPingAsync(m_CurrentLobby.Id);
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

        void OnApplicationQuit()
        {
            // Just leave the lobby, keep it running as we need it for the other clients for host migration
            if (m_CurrentLobby != null)
                LobbyService.Instance.RemovePlayerAsync(m_CurrentLobby.Id, AuthenticationService.Instance.PlayerId);
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (m_Heartbeat != null)
                StopCoroutine(m_Heartbeat);
            if (m_CurrentLobby != null && !string.IsNullOrEmpty(m_CurrentLobby.Id))
                LobbyService.Instance.RemovePlayerAsync(m_CurrentLobby.Id, AuthenticationService.Instance.PlayerId);
        }
#endif
    }
}
