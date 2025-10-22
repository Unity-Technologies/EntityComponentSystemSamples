using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport.Relay;

namespace Samples.HelloNetcode
{
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
}
