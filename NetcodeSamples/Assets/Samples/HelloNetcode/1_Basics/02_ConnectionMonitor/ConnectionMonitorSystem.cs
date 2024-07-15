using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Samples.HelloNetcode
{
    [UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial class ConnectionMonitorSystem : SystemBase
    {
        private BeginSimulationEntityCommandBufferSystem m_CommandBufferSystem;
        private ConnectionUI m_ConnectionUI;
        private Button.ButtonClickedEvent m_ButtonPressed;
        static readonly FixedString32Bytes[] s_ConnectionState =
        {
            "Unknown",
            "Disconnected",
            "Connecting",
            "Handshake",
            "Connected"
        };

        protected override void OnCreate()
        {
            RequireForUpdate<EnableConnectionMonitor>();
            m_CommandBufferSystem = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            if (m_ConnectionUI == null)
                m_ConnectionUI = GameObject.FindFirstObjectByType<ConnectionUI>();

            var buffer = m_CommandBufferSystem.CreateCommandBuffer();
            Entities.WithName("AddConnectionStateToNewConnections").WithNone<ConnectionState>().ForEach((Entity entity,
                in NetworkStreamConnection state) =>
            {
                buffer.AddComponent<ConnectionState>(entity);
            }).Run();

            FixedString32Bytes worldName = World.Name;
            var unmanagedWorld = World.Unmanaged;
            // Buttons are laid out in columns according to worlds, Server,ClientWorld0,ClientWorld1 and so on
            int worldIndex = 0;
            if (int.TryParse(World.Name[World.Name.Length - 1].ToString(), out worldIndex))
                worldIndex++;
            Entities.WithName("InitializeNewConnection").WithNone<InitializedConnection>().ForEach((Entity entity, in NetworkId id) =>
            {
                buffer.AddComponent(entity, new InitializedConnection());
                UnityEngine.Debug.Log($"[{worldName}] New connection ID:{id.Value}");

                // Not thread safe, so all UI logic is kept on main thread
                ConnectionMonitorUIData.Connections.Data.Enqueue(new Connection(){Id = id.Value, WorldIndex = worldIndex, World = unmanagedWorld});
            }).Run();

            Entities.WithName("HandleDisconnect").WithNone<NetworkStreamConnection>().ForEach((Entity entity, in ConnectionState state) =>
            {
                UnityEngine.Debug.Log($"[{worldName}] Connection disconnected ID:{state.NetworkId} Reason:{state.DisconnectReason.ToFixedString()}");

                // Not thread safe, so all UI logic is kept on main thread
                ConnectionMonitorUIData.Connections.Data.Enqueue(new Connection(){Id = state.NetworkId, WorldIndex = worldIndex, World = unmanagedWorld, ConnectionDeleted = true});
                buffer.RemoveComponent<ConnectionState>(entity);
            }).Run();

            m_CommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }

    public class DriverConstructor : INetworkStreamDriverConstructor
    {
        private static readonly int s_DisconnectTimeout = 2000;

        private NetworkSettings CreateNetworkSettings(int maxFrameTime = 0)
        {
            var settings = new NetworkSettings();
            settings.WithNetworkConfigParameters(
                connectTimeoutMS: 1000,
                disconnectTimeoutMS: s_DisconnectTimeout,
                heartbeatTimeoutMS: s_DisconnectTimeout / 2,
                fixedFrameTimeMS: 0,
                maxFrameTimeMS: maxFrameTime);
            settings.WithReliableStageParameters(windowSize: 32)
                .WithFragmentationStageParameters(payloadCapacity: 16 * 1024);
            return settings;
        }

        private void CreateDriver(out NetworkDriver driver, NetworkSettings settings)
        {
#if !UNITY_WEBGL
            driver = NetworkDriver.Create(new UDPNetworkInterface(), settings);
#else
            driver = NetworkDriver.Create(new WebSocketNetworkInterface(), settings);
#endif
        }

        public void CreateClientDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
            var driverInstance = new NetworkDriverStore.NetworkDriverInstance();
#if UNITY_EDITOR || NETCODE_DEBUG
            var settings = CreateNetworkSettings(100);
            driverInstance.simulatorEnabled = NetworkSimulatorSettings.Enabled;
            if (NetworkSimulatorSettings.Enabled)
            {
                NetworkSimulatorSettings.SetSimulatorSettings(ref settings);
                CreateDriver(out driverInstance.driver, settings);
                DefaultDriverBuilder.CreateClientSimulatorPipelines(ref driverInstance);
            }
            else
            {
                CreateDriver(out driverInstance.driver, settings);
                DefaultDriverBuilder.CreateClientPipelines(ref driverInstance);
            }
#else
            var settings = CreateNetworkSettings();
            CreateDriver(out driverInstance.driver, settings);
            DefaultDriverBuilder.CreateClientPipelines(ref driverInstance);
#endif
            driverStore.RegisterDriver(TransportType.Socket, driverInstance);
        }

        public void CreateServerDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
#if UNITY_EDITOR || !UNITY_WEBGL
            var settings = CreateNetworkSettings();
            var driverInstance = new NetworkDriverStore.NetworkDriverInstance();
            CreateDriver(out driverInstance.driver, settings);
            DefaultDriverBuilder.CreateServerPipelines(ref driverInstance);
            driverStore.RegisterDriver(TransportType.Socket, driverInstance);
#else
            throw new System.NotSupportedException("It is not allowed to create a server NetworkDriver for WebGL build.");
#endif
        }
    }

    // Management for the queue which passes data between DOTS and GameObject systems, this way
    // the two are decoupled a bit cleaner
    [UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class ConnectionMonitor_UIDataSystem : SystemBase
    {
        private bool m_OwnsData;
        protected override void OnCreate()
        {
            m_OwnsData = !ConnectionMonitorUIData.Connections.Data.IsCreated;
            if (m_OwnsData)
                ConnectionMonitorUIData.Connections.Data = new UnsafeRingQueue<Connection>(32, Allocator.Persistent);
            Enabled = false;
        }

        protected override void OnUpdate() { }

        protected override void OnDestroy()
        {
            if (m_OwnsData)
                ConnectionMonitorUIData.Connections.Data.Dispose();
        }
    }

    public struct Connection
    {
        public int Id;
        public int WorldIndex;
        public WorldUnmanaged World;
        public bool ConnectionDeleted;
    }

    public abstract class ConnectionMonitorUIData
    {
        public static readonly SharedStatic<UnsafeRingQueue<Connection>> Connections = SharedStatic<UnsafeRingQueue<Connection>>.GetOrCreate<ConnectionKey>();

        // Identifiers for the shared static fields
        private class ConnectionKey {}
    }
}
