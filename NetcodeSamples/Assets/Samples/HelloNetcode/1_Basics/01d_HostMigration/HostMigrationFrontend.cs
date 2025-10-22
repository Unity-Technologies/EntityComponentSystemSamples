using System;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Samples.HelloNetcode
{
    public class HostMigrationFrontend :
#if UNITY_SERVER
        MonoBehaviour
#else
        Frontend
#endif
    {
        public string HostConnectionStatus
        {
            get => HostConnectionLabel.text;
            set => HostConnectionLabel.text = value;
        }
        public string ClientConnectionStatus
        {
            get => ClientConnectionLabel.text;
            set => ClientConnectionLabel.text = value;
        }

        public Button JoinExistingGame;
        public Text HostConnectionLabel;
        public Text ClientConnectionLabel;
        public Toggle UseRelayForLocalConnection;
        public Toggle EnableRelay;

        // The time, in seconds, we'll wait for a host join code when doing the initial connect
        const int k_InitialHostJoinWaitTimeout = 30;
        const string k_LobbyName = "TestLobby";
        string m_PreviousAddressText;
        string m_OldValue;
        ConnectionState m_State;

        public GameObject hostMigrationControllerPrefab;
        public HostMigrationController hostMigrationController;

#if !UNITY_SERVER
        protected override void OnStart()
        {
            hostMigrationController = FindFirstObjectByType<HostMigrationController>();
            if (hostMigrationController == null)
                hostMigrationController = Instantiate(hostMigrationControllerPrefab).GetComponent<HostMigrationController>();
        }

        public void OnEnableHostMigration(Toggle value)
        {
            if (hostMigrationController == null)
                hostMigrationController = FindFirstObjectByType<HostMigrationController>();
            DontDestroyOnLoad(hostMigrationController);
            EnableRelay.gameObject.SetActive(!value.isOn);
            UseRelayForLocalConnection.gameObject.SetActive(!value.isOn);
            Port.gameObject.SetActive(!value.isOn);
            TogglePersistentState(!value.isOn);
            if (value.isOn)
            {
                m_PreviousAddressText = Address.text;
                Address.text = k_LobbyName;
                ClientServerButton.onClick.AddListener(SetupRelayAndLobbyAsHost);
                JoinExistingGame.onClick.AddListener(JoinLobbyAndConnectWithRelayAsClient);
                SceneManager.sceneLoaded += hostMigrationController.OnSceneLoaded;
            }
            else
            {
                Address.text = m_PreviousAddressText;
                ClientServerButton.onClick.RemoveAllListeners();
                JoinExistingGame.onClick.RemoveAllListeners();
                SceneManager.sceneLoaded -= hostMigrationController.OnSceneLoaded;
            }
        }

        void TogglePersistentState(bool shouldListen)
        {
            if (shouldListen)
            {
                ClientServerButton.onClick.SetPersistentListenerState(0, UnityEventCallState.RuntimeOnly);
                JoinExistingGame.onClick.SetPersistentListenerState(0, UnityEventCallState.RuntimeOnly);
            }
            else
            {
                ClientServerButton.onClick.SetPersistentListenerState(0, UnityEventCallState.Off);
                JoinExistingGame.onClick.SetPersistentListenerState(0, UnityEventCallState.Off);
            }
        }

        /// <summary>
        /// Initial setup for the host of the game session.
        /// </summary>
        async void SetupRelayAndLobbyAsHost()
        {
            HostConnectionStatus = "Initializing services";
            await InitializeServices();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                HostConnectionStatus = "Logging in anonymously";
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            Debug.Log($"[HostMigration] Signed in as {AuthenticationService.Instance.PlayerId}");

            HostConnectionStatus = "Waiting for allocation";
            var allocation = await RelayService.Instance.CreateAllocationAsync(hostMigrationController.MaxPlayers - 1);
            HostConnectionStatus = "Waiting for join code";
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"[HostMigration] Created allocation {allocation.AllocationId} with join code {joinCode}");

            SetupRelayHostedServerAndConnect(allocation.ToRelayServerData(hostMigrationController.ConnectionType));
            Debug.Log($"[HostMigration] Creating lobby with name {Address.text}");
            await hostMigrationController.CreateLobbyAsync(joinCode, allocation.AllocationId.ToString());
            await hostMigrationController.SubscribeToLobbyEvents();
        }

        /// <summary>
        /// Initial service initialization. This only needs to be done once. When running in a standalone player
        /// we need to use a different player profile or else the lobby will treat this player as the same identity
        /// as the editor or other players (would remove everyone from the lobby for example when one player instance
        /// disconnects/leaves). A random profile name will be generated unless the -userprofile argument is passed
        /// to the process in which case the given name will be used.
        /// </summary>
        async Task InitializeServices()
        {
            var userprofile = CommandLineUtils.GetCommandLineValueFromKey("userprofile");
            if (!string.IsNullOrEmpty(userprofile))
            {
                Debug.Log($"[HostMigration] Using user profile {userprofile}");
                var options = new InitializationOptions();
                options.SetProfile(userprofile);
                await UnityServices.InitializeAsync(options);
            }
            else if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.LinuxPlayer)
            {
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
        /// Initial setup for clients joining a game session.
        /// </summary>
        async void JoinLobbyAndConnectWithRelayAsClient()
        {
            ClientConnectionStatus = "Initializing services";
            await InitializeServices();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                ClientConnectionStatus = "Logging in anonymously";
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            Debug.Log($"[HostMigration] Signed in as {AuthenticationService.Instance.PlayerId}");

            ClientConnectionStatus = "Joining lobby";
            await hostMigrationController.JoinLobbyByNameAsync(Address.text);
            await hostMigrationController.SubscribeToLobbyEvents();

            if (hostMigrationController.CurrentLobby != null && hostMigrationController.CurrentLobby.Players != null)
            {
                if (!hostMigrationController.CurrentLobby.Data.ContainsKey(LobbyKeys.RelayHost) ||
                    !hostMigrationController.CurrentLobby.Data.ContainsKey(LobbyKeys.RelayJoinCode))
                {
                    Debug.LogError($"[HostMigration] Lobby data missing entries for '{LobbyKeys.RelayHost}' and/or '{LobbyKeys.RelayJoinCode}'");
                    return;
                }

                // If the lobby data has invalid joincode (probably the previous host) then it has not
                // been updated by the current host. A migration is likely taking place and we should wait for
                // the proper join code as normally clients do during a host migration
                if (hostMigrationController.CurrentLobby.Data[LobbyKeys.RelayHost].Value !=
                    hostMigrationController.CurrentLobby.HostId)
                {
                    Debug.Log("[HostMigration] Relay host does not match lobby host. Will wait for join code");
                    ClientConnectionStatus = "Waiting for join code";
                    hostMigrationController.WaitForInitialJoin = true;
                    var timeout = Time.realtimeSinceStartup + k_InitialHostJoinWaitTimeout;
                    while (hostMigrationController.CurrentLobby.Data[LobbyKeys.RelayHost].Value != hostMigrationController.CurrentLobby.HostId)
                    {
                        if (Time.realtimeSinceStartup > timeout)
                        {
                            Debug.LogError("[HostMigration] Relay host does not match lobby host, timeout while waiting for updated join code.");
                            hostMigrationController.WaitForInitialJoin = false;
                            return;
                        }
                        await Task.Delay(100);
                    }
                    hostMigrationController.WaitForInitialJoin = false;
                    if (hostMigrationController.CurrentLobby.Data[LobbyKeys.RelayHost].Value == AuthenticationService.Instance.PlayerId)
                    {
                        Debug.Log("Elected as host while trying to join as client. Will abort the client join flow as the host flow from host migration election will take over.");
                        return;
                    }
                }
                var relayJoinCode = hostMigrationController.CurrentLobby.Data[LobbyKeys.RelayJoinCode].Value;
                Debug.Log($"[HostMigration] Using relay join code {relayJoinCode} from lobby data");
                hostMigrationController.RelayJoinCode = relayJoinCode;
                ClientConnectionStatus = "Joining relay allocation";
                var allocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
                ClientConnectionStatus = "Binding to relay server";
                ConnectToServerWithRelay(allocation.ToRelayServerData(hostMigrationController.ConnectionType));
                await hostMigrationController.UpdatePlayerAllocationId(allocation.AllocationId);
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

                LoadScenes(server);

                using var driverQuery = server.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                var serverDriver = driverQuery.GetSingletonRW<NetworkStreamDriver>();
                serverDriver.ValueRW.RequireConnectionApproval = m_RequireConnectionApproval;
                if (!serverDriver.ValueRW
                        .Listen(NetworkEndpoint.AnyIpv4))
                {
                    Debug.LogError($"NetworkStreamDriver.Listen() failed");
                    return;
                }

                // Update the UI with the status of connecting to the relay
                var relayEntity = ClientServerBootstrap.ServerWorld.EntityManager.CreateEntity(ComponentType.ReadOnly<WaitForRelayConnection>());
                ClientServerBootstrap.ServerWorld.EntityManager.SetComponentData(relayEntity, new WaitForRelayConnection() { StartTime = Time.realtimeSinceStartup });

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

                LoadScenes(client);

                // Update the UI with the status of connecting to the relay
                var relayEntity = client.EntityManager.CreateEntity(ComponentType.ReadOnly<WaitForRelayConnection>());
                client.EntityManager.SetComponentData(relayEntity, new WaitForRelayConnection() { StartTime = Time.realtimeSinceStartup });

                var networkStreamEntity = client.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestConnect>());
                client.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestConnect");

                // For IPC this will not work and give an error in the transport layer. For this sample we force the client to connect through the relay service.
                // For a locally hosted server, the client would need to connect to NetworkEndpoint.AnyIpv4, and the relayClientData.Endpoint in all other cases.
                client.EntityManager.SetComponentData(networkStreamEntity, new NetworkStreamRequestConnect { Endpoint = relayServerData.Endpoint });
            }
            finally
            {
                NetworkStreamReceiveSystem.DriverConstructor = oldConstructor;
            }
        }

        /// <summary>
        /// Load all scenes we'll be using, the normal frontend HUD UI scene, the selected sample scene and the
        /// host migration HUD UI scene. The local simulation world will be destroyed and the default game object
        /// injection world replaced with the world parameter.
        /// </summary>
        public void LoadScenes(World defaultInjectionWorld)
        {
            SceneManager.LoadScene("FrontendHUD");

            //Destroy the local simulation world to avoid the game scene to be loaded into it
            //This prevent rendering (rendering from multiple world with presentation is not greatly supported)
            //and other issues.
            DestroyLocalSimulationWorld();
            if (World.DefaultGameObjectInjectionWorld == null)
                World.DefaultGameObjectInjectionWorld = defaultInjectionWorld;

            var sceneName = GetAndSaveSceneSelection();
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
            SceneManager.LoadScene("HostMigrationHUD", LoadSceneMode.Additive);
        }
#endif
    }
}
