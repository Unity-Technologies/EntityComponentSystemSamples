using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport.Relay;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Samples.HelloNetcode
{
    /// <summary>
    /// This driver constructor and network handler are the same as the default ones
    /// from the Multiplayer SDK except the network handler contains two fixes. One where
    /// the default driver constructor is overwritten but not replaced back after use, so
    /// if you use sessions once then it will the be session driver constructor used next time
    /// unless the default driver constructor is again set. And another where a query
    /// checking for network disconnects doesn't verify the world still exists after waiting
    /// for a delay and re-checking (will result in crash if world was destroyed while waiting).
    /// </summary>
    class CustomDriverConstructor : INetworkStreamDriverConstructor
    {
        public NetworkConfiguration Configuration;
        public const int InvalidDriverId = 0;

        public int ClientIpcDriverId { get; private set; } = InvalidDriverId;
        public int ClientUdpDriverId { get; private set; } = InvalidDriverId;
        public int ClientWebSocketDriverId { get; private set; } = InvalidDriverId;

        public int ServerIpcDriverId { get; private set; } = InvalidDriverId;
        public int ServerUdpDriverId { get; private set; } = InvalidDriverId;
        public int ServerWebSocketDriverId { get; private set; } = InvalidDriverId;

        public void CreateClientDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
            var ipcSettings = DefaultDriverBuilder.GetNetworkSettings();

            var driverId = 1;

            if (Configuration.Role == NetworkRole.Host || Configuration.Role == NetworkRole.Server)
            {
                DefaultDriverBuilder.RegisterClientIpcDriver(world, ref driverStore, netDebug, ipcSettings);
                ClientIpcDriverId = driverId;
            }
            else if (Configuration.Role == NetworkRole.Client)
            {
                var udpSettings = DefaultDriverBuilder.GetNetworkSettings();

                if (Configuration.Type == NetworkType.Relay)
                {
                    var relayClientData = Configuration.RelayClientData;
                    udpSettings.WithRelayParameters(ref relayClientData);
                }

#if !UNITY_WEBGL
                DefaultDriverBuilder.RegisterClientUdpDriver(world, ref driverStore, netDebug, udpSettings);
#else
                DefaultDriverBuilder.RegisterClientWebSocketDriver(world, ref driverStore, netDebug, udpSettings);
#endif
                ClientUdpDriverId = driverId;
            }
        }

        public void CreateServerDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
            var ipcSettings = DefaultDriverBuilder.GetNetworkSettings();

            var driverId = 1;

            if (Configuration.Role == NetworkRole.Host)
            {
                DefaultDriverBuilder.RegisterServerIpcDriver(world, ref driverStore, netDebug, ipcSettings);
                ServerIpcDriverId = driverId;
                driverId++;
            }

            var udpSettings = DefaultDriverBuilder.GetNetworkSettings();

            if (Configuration.Type == NetworkType.Relay)
            {
                var relayServerData = Configuration.RelayServerData;
                udpSettings.WithRelayParameters(ref relayServerData);
            }

#if !UNITY_WEBGL
            DefaultDriverBuilder.RegisterServerUdpDriver(world, ref driverStore, netDebug, udpSettings);
#else
            DefaultDriverBuilder.RegisterServerWebSocketDriver(world, ref driverStore, netDebug, udpSettings);
#endif
            ServerUdpDriverId = driverId;
        }
    }

    class CustomNetcodeNetworkHandler : INetworkHandler
    {
        const int ConnectionTimeoutSeconds = 10;

        NetworkConfiguration m_Configuration;
        World m_ClientWorld;
        World m_ServerWorld;

        NetworkStreamDriver m_ClientDriver;
        NetworkStreamDriver m_ServerDriver;
        Entity m_ConnectionEntity;

        INetworkStreamDriverConstructor m_OldDriverConstructor;
        readonly CustomDriverConstructor m_DriverConstructor = new CustomDriverConstructor();

        public async Task StartAsync(NetworkConfiguration configuration)
        {
            m_Configuration = configuration;

            SetupWorlds();
            ValidateWorlds();
            SetupDriverConstructor();

            switch (m_Configuration.Role)
            {
                case NetworkRole.Client:
                    await ConnectAsync();
                    break;
                case NetworkRole.Host:
                    Listen();
                    await SelfConnectAsync();
                    break;
                case NetworkRole.Server:
                    Listen();
                    break;
            }
        }

        public async Task StopAsync()
        {
            CleanupServer();
            await CleanupClientAsync();
            NetworkStreamReceiveSystem.DriverConstructor = m_OldDriverConstructor;
        }

        void SetupWorlds()
        {
            m_ClientWorld = ClientServerBootstrap.ClientWorld;
            m_ServerWorld = ClientServerBootstrap.ServerWorld;

            if ((m_Configuration.Role is NetworkRole.Client or NetworkRole.Host) && m_ClientWorld == null)
            {
                m_ClientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
            }

            if ((m_Configuration.Role is NetworkRole.Host or NetworkRole.Server) && m_ServerWorld == null)
            {
                m_ServerWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
            }
        }

        void SetupDriverConstructor()
        {
            m_DriverConstructor.Configuration = m_Configuration;
            m_OldDriverConstructor = NetworkStreamReceiveSystem.DriverConstructor;
            NetworkStreamReceiveSystem.DriverConstructor = m_DriverConstructor;
        }

        void SetupClientDriver()
        {
            using var drvQuery = m_ClientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            m_ClientDriver = drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW;

            using (var debugQuery = m_ClientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetDebug>()))
            {
                var netDebug = debugQuery.GetSingleton<NetDebug>();
                var driverStore = new NetworkDriverStore();
                NetworkStreamReceiveSystem.DriverConstructor.CreateClientDriver(m_ClientWorld, ref driverStore, netDebug);
                m_ClientDriver.ResetDriverStore(m_ClientWorld.Unmanaged, ref driverStore);
            }
        }

        void SetupServerDriver()
        {
            using var drvQuery = m_ServerWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            m_ServerDriver = drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW;

            using (var debugQuery = m_ServerWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetDebug>()))
            {
                var netDebug = debugQuery.GetSingleton<NetDebug>();
                var driverStore = new NetworkDriverStore();
                NetworkStreamReceiveSystem.DriverConstructor.CreateServerDriver(m_ServerWorld, ref driverStore, netDebug);
                m_ServerDriver.ResetDriverStore(m_ServerWorld.Unmanaged, ref driverStore);
            }
        }

        void Listen()
        {
            ValidateWorld(m_ServerWorld);
            SetupServerDriver();

            Unity.Networking.Transport.NetworkEndpoint listenEndpoint = default;

            switch (m_Configuration.Type)
            {
                case NetworkType.Direct:
                    listenEndpoint = m_Configuration.DirectNetworkListenAddress;
                    break;
                case NetworkType.Relay:
                    listenEndpoint = Unity.Networking.Transport.NetworkEndpoint.AnyIpv4;
                    break;
            }

            if (!listenEndpoint.IsValid)
            {
                throw new Exception("Invalid endpoint to listen to");
            }

            if (m_ServerDriver.Listen(listenEndpoint))
            {
                var serverUdpPort = m_ServerDriver.GetLocalEndPoint(m_DriverConstructor.ServerUdpDriverId).Port;

                if (m_Configuration.Type == NetworkType.Direct)
                {
                    m_Configuration.UpdatePublishPort(serverUdpPort);
                }
            }
            else
            {
                throw new Exception("We assume the first driver created is IPC Network Interface! Check your `INetworkStreamDriverConstructor` implementation (hooked up via `NetworkStreamReceiveSystem.DriverConstructor`).");
            }
        }

        async Task SelfConnectAsync()
        {
            ValidateWorld(m_ClientWorld);
            SetupClientDriver();

            var ipcPort = m_ServerDriver.GetLocalEndPoint(m_DriverConstructor.ServerIpcDriverId).Port;
            var selfEndpoint = Unity.Networking.Transport.NetworkEndpoint.LoopbackIpv4.WithPort(ipcPort);
            m_ConnectionEntity = m_ClientDriver.Connect(m_ClientWorld.EntityManager, selfEndpoint);

            await ValidateConnectionAsync();
        }

        async Task ConnectAsync()
        {
            ValidateWorld(m_ClientWorld);
            SetupClientDriver();

            Unity.Networking.Transport.NetworkEndpoint connectEndpoint = default;

            switch (m_Configuration.Type)
            {
                case NetworkType.Direct:
                {
                    connectEndpoint = m_Configuration.DirectNetworkPublishAddress;
                    break;
                }
                case NetworkType.Relay:
                {
                    connectEndpoint = m_Configuration.RelayClientData.Endpoint;
                    break;
                }
            }

            if (!connectEndpoint.IsValid)
            {
                throw new Exception("Invalid endpoint to connect to");
            }

            m_ConnectionEntity = m_ClientDriver.Connect(m_ClientWorld.EntityManager, connectEndpoint);
            await ValidateConnectionAsync();
        }

        async Task ValidateConnectionAsync()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var connection = m_ClientWorld.EntityManager.GetComponentData<NetworkStreamConnection>(m_ConnectionEntity);

            while (connection.CurrentState != ConnectionState.State.Connected &&
                   connection.CurrentState != ConnectionState.State.Disconnected)
            {
                if (stopwatch.Elapsed.TotalSeconds >= ConnectionTimeoutSeconds)
                {
                    await CleanupClientAsync();
                    throw new Exception("Connection timeout. Failed connection");
                }

                await Task.Delay(100);

                if (!m_ClientWorld.EntityManager.Exists(m_ConnectionEntity))
                {
                    throw new Exception("Connect Entity no longer exists. Failed connection");
                }

                connection = m_ClientWorld.EntityManager.GetComponentData<NetworkStreamConnection>(m_ConnectionEntity);
            }
        }

        async Task CleanupClientAsync()
        {
            if (m_ClientWorld != null &&
                m_ClientWorld.IsCreated &&
                m_ClientWorld.EntityManager.Exists(m_ConnectionEntity))
            {
                m_ClientWorld.EntityManager.AddComponent<NetworkStreamRequestDisconnect>(m_ConnectionEntity);
                m_ConnectionEntity = default;

                var connectionQuery = m_ClientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkStreamConnection>());
                while (!connectionQuery.IsEmpty)
                {
                    await Task.Delay(100);
                    if (m_ClientWorld == null || !m_ClientWorld.IsCreated)
                    {
                        return; // if world is gone after the yield immediately return, the query will already be disposed
                    }
                }
                connectionQuery.Dispose();
            }
        }

        void CleanupServer()
        {
            if (m_ServerWorld != null)
            {
                m_ServerWorld.Dispose();
                m_ServerWorld = null;
            }
        }

        void ValidateWorlds()
        {
            if ((m_Configuration.Role is NetworkRole.Client or NetworkRole.Host) && m_ClientWorld is not { IsCreated : true })
            {
                throw new Exception("Invalid client world. Please make sure it has been created before attempting to setup network.");
            }
            if ((m_Configuration.Role is NetworkRole.Server or NetworkRole.Host) && m_ServerWorld is not { IsCreated : true })
            {
                throw new Exception("Invalid server world. Please make sure it has been created before attempting to setup network.");
            }
        }

        void ValidateWorld(World world)
        {
            if (world == null || !world.IsCreated)
            {
                throw new Exception("Invalid world to setup network");
            }
        }
    }
}
