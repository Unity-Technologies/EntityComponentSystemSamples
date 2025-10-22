using System;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Samples.HelloNetcode
{
    /// <summary>
    /// HUD implementation. Implements behaviour for the buttons, hosting server, joining client, and starting game.
    ///
    /// Text fields output the status of server and client registering with the relay server once the user presses
    /// the respective buttons.
    ///
    /// A bootstrap world is constructed to run the jobs for setting up host and client configuration for relay server.
    /// Once this is done the game can be launched and the configuration can be retrieved from the constructed world.
    /// </summary>
    public class RelayFrontend :
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
        public Toggle EnableHostMigration;

        string m_OldValue;
        bool m_UseRelayForLocalClient;
        bool m_IsHosting;
        ConnectionState m_State;
        HostServer m_HostServerSystem;
        ConnectingPlayer m_HostClientSystem;

        enum ConnectionState
        {
            Unknown,
            SetupHost,
            SetupClient,
            JoinGame,
            JoinLocalGame,
        }

#if !UNITY_SERVER
        public override void Start()
        {
            base.Start();
#if UNITY_WEBGL
            ClientServerButton.interactable = false;
#endif
        }
        public void OnUseRelayForLocalClient(Toggle value)
        {
            m_UseRelayForLocalClient = value.isOn;
        }
        public void OnRelayEnable(Toggle value)
        {
            TogglePersistentState(!value.isOn);
            if (value.isOn)
            {
                Port.gameObject.SetActive(false);
                EnableHostMigration.gameObject.SetActive(false);
                UseRelayForLocalConnection.interactable = true;
                m_OldValue = Address.text;
                Address.text = string.Empty;
                Address.placeholder.GetComponent<Text>().text = "Join Code for Host Server";
                ClientServerButton.interactable = true;
                ClientServerButton.onClick.AddListener(() => { m_State = ConnectionState.SetupHost; });
                JoinExistingGame.onClick.AddListener(() => { m_State = ConnectionState.SetupClient; });
            }
            else
            {
                Port.gameObject.SetActive(true);
                EnableHostMigration.gameObject.SetActive(true);
                UseRelayForLocalConnection.interactable = false;
                Address.text = m_OldValue;
                Address.placeholder.GetComponent<Text>().text = string.Empty;
#if UNITY_WEBGL
                ClientServerButton.interactable = false;
#endif
                ClientServerButton.onClick.RemoveAllListeners();
                JoinExistingGame.onClick.RemoveAllListeners();
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

        public void Update()
        {
            // When relay is toggled on the hosting button should be disabled when the user enters something in
            // the join code text field, as it's expected you'd want to join a relay session next
            if (EnableRelay.isOn)
            {
                if (!string.IsNullOrEmpty(Address.text))
                    ClientServerButton.interactable = false;
                else
                    ClientServerButton.interactable = true;
            }
            // If relay is toggled off then reset hosting button, unless you're on webgl then it should
            // remain off as it only supports hosting on relay
            else if (!ClientServerButton.interactable)
            {
#if !UNITY_WEBGL
                ClientServerButton.interactable = true;
#endif
            }

            switch (m_State)
            {
                case ConnectionState.SetupHost:
                {
                    m_IsHosting = true;
                    HostServer();
                    m_State = ConnectionState.SetupClient;
                    goto case ConnectionState.SetupClient;
                }
                case ConnectionState.SetupClient:
                {
                    var isServerHostedLocally = m_HostServerSystem?.RelayServerData.Endpoint.IsValid;
                    var enteredJoinCode = !string.IsNullOrEmpty(Address.text);
                    if (isServerHostedLocally.GetValueOrDefault())
                    {
                        if (m_UseRelayForLocalClient)
                        {
                            SetupClient();
                            m_HostClientSystem.GetJoinCodeFromHost();
                        }
                        m_State = ConnectionState.JoinLocalGame;
                        goto case ConnectionState.JoinLocalGame;
                    }

                    if (enteredJoinCode)
                    {
                        JoinAsClient();
                        m_State = ConnectionState.JoinGame;
                        goto case ConnectionState.JoinGame;
                    }

                    if (!m_IsHosting)
                    {
                        ClientConnectionLabel.text = "Join Code field is empty!";
                        m_State = ConnectionState.Unknown;
                    }
                    break;
                }
                case ConnectionState.JoinGame:
                {
                    var hasClientConnectedToRelayService = m_HostClientSystem?.RelayClientData.Endpoint.IsValid;
                    if (hasClientConnectedToRelayService.GetValueOrDefault())
                    {
                        ConnectToRelayServer();
                        m_State = ConnectionState.Unknown;
                    }
                    break;
                }
                case ConnectionState.JoinLocalGame:
                {
                    var hasClientConnectedToRelayService = m_HostClientSystem?.RelayClientData.Endpoint.IsValid;
                    if (!m_UseRelayForLocalClient || hasClientConnectedToRelayService.GetValueOrDefault())
                    {
                        SetupRelayHostedServerAndConnect();
                        m_State = ConnectionState.Unknown;
                    }
                    break;
                }
                case ConnectionState.Unknown:
                {
                    m_IsHosting = false;
                    break;
                }
                default: return;
            }
        }

        void HostServer()
        {
            var world = World.All[0];
            m_HostServerSystem = world.GetOrCreateSystemManaged<HostServer>();
            var enableRelayServerEntity = world.EntityManager.CreateEntity(ComponentType.ReadWrite<EnableRelayServer>());
            world.EntityManager.AddComponent<EnableRelayServer>(enableRelayServerEntity);

            m_HostServerSystem.UIBehaviour = this;
            var simGroup = world.GetExistingSystemManaged<SimulationSystemGroup>();
            simGroup.AddSystemToUpdateList(m_HostServerSystem);
        }

        void SetupClient()
        {
            var world = World.All[0];
            m_HostClientSystem = world.GetOrCreateSystemManaged<ConnectingPlayer>();
            m_HostClientSystem.UIBehaviour = this;
            var simGroup = world.GetExistingSystemManaged<SimulationSystemGroup>();
            simGroup.AddSystemToUpdateList(m_HostClientSystem);
        }

        void JoinAsClient()
        {
            SetupClient();
            var world = World.All[0];
            var enableRelayServerEntity = world.EntityManager.CreateEntity(ComponentType.ReadWrite<EnableRelayServer>());
            world.EntityManager.AddComponent<EnableRelayServer>(enableRelayServerEntity);
            m_HostClientSystem.JoinUsingCode(Address.text);
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
        void SetupRelayHostedServerAndConnect()
        {
            if (ClientServerBootstrap.RequestedPlayType != ClientServerBootstrap.PlayType.ClientAndServer)
            {
                UnityEngine.Debug.LogError($"Creating client/server worlds is not allowed if playmode is set to {ClientServerBootstrap.RequestedPlayType}");
                return;
            }

            var world = World.All[0];
            var relayClientData = world.GetExistingSystemManaged<ConnectingPlayer>()?.RelayClientData;
            var relayServerData = world.GetExistingSystemManaged<HostServer>().RelayServerData;
            var joinCode = world.GetExistingSystemManaged<HostServer>().JoinCode;

            var oldConstructor = NetworkStreamReceiveSystem.DriverConstructor;
            NetworkStreamReceiveSystem.DriverConstructor = new RelayDriverConstructor(relayServerData, relayClientData.GetValueOrDefault());
            var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
            var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
            NetworkStreamReceiveSystem.DriverConstructor = oldConstructor;

            SceneManager.LoadScene("FrontendHUD");
            SceneManager.LoadScene("RelayHUD", LoadSceneMode.Additive);

            //Destroy the local simulation world to avoid the game scene to be loaded into it
            //This prevent rendering (rendering from multiple world with presentation is not greatly supported)
            //and other issues.
            DestroyLocalSimulationWorld();
            if (World.DefaultGameObjectInjectionWorld == null)
                World.DefaultGameObjectInjectionWorld = server;

            SceneManager.LoadSceneAsync(GetAndSaveSceneSelection(), LoadSceneMode.Additive);

            var joinCodeEntity = server.EntityManager.CreateEntity(ComponentType.ReadOnly<JoinCode>());
            server.EntityManager.SetComponentData(joinCodeEntity, new JoinCode { Value = joinCode });

            using var serverDriverQuery = server.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver));
            var serverDriver = serverDriverQuery.GetSingletonRW<NetworkStreamDriver>();
            serverDriver.ValueRW.RequireConnectionApproval = m_RequireConnectionApproval;
            serverDriver.ValueRW.Listen(NetworkEndpoint.AnyIpv4);
            var ipcLocalEndPoint = serverDriver.ValueRW.DriverStore.GetDriverInstanceRO(1).driver.GetLocalEndpoint();

            using var clientDriverQuery = client.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver));
            var clientDriver = clientDriverQuery.GetSingletonRW<NetworkStreamDriver>();
            if (relayClientData.HasValue)
            {
                clientDriver.ValueRW.Connect(client.EntityManager, relayClientData.Value.Endpoint);
            }
            else
            {
                clientDriver.ValueRW.Connect(client.EntityManager, ipcLocalEndPoint);
            }
        }

        void ConnectToRelayServer()
        {
            var world = World.All[0];
            var relayClientData = world.GetExistingSystemManaged<ConnectingPlayer>().RelayClientData;

            var oldConstructor = NetworkStreamReceiveSystem.DriverConstructor;
            NetworkStreamReceiveSystem.DriverConstructor = new RelayDriverConstructor(new RelayServerData(), relayClientData);
            var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
            NetworkStreamReceiveSystem.DriverConstructor = oldConstructor;

            SceneManager.LoadScene("FrontendHUD");

            //Destroy the local simulation world to avoid the game scene to be loaded into it
            //This prevent rendering (rendering from multiple world with presentation is not greatly supported)
            //and other issues.
            DestroyLocalSimulationWorld();
            if (World.DefaultGameObjectInjectionWorld == null)
                World.DefaultGameObjectInjectionWorld = client;

            SceneManager.LoadSceneAsync(GetAndSaveSceneSelection(), LoadSceneMode.Additive);

            var networkStreamEntity = client.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestConnect>());
            client.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestConnect");
            // For IPC this will not work and give an error in the transport layer. For this sample we force the client to connect through the relay service.
            // For a locally hosted server, the client would need to connect to NetworkEndpoint.AnyIpv4, and the relayClientData.Endpoint in all other cases.
            client.EntityManager.SetComponentData(networkStreamEntity, new NetworkStreamRequestConnect { Endpoint = relayClientData.Endpoint });
        }
#endif
    }
}
