using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport.Relay;

namespace Samples.HelloNetcode
{
    /// <summary>
    /// Register client and server using relay server settings.
    ///
    /// Settings are retrieved from bootstrap world. This driver constructor will run when pressing 'Start Game'
    /// and should only be pressed after both server and client configuration has been properly initialized.
    /// </summary>
    public class RelayDriverConstructor : INetworkStreamDriverConstructor
    {
        RelayServerData m_RelayClientData;
        RelayServerData m_RelayServerData;

        public RelayDriverConstructor(RelayServerData serverData, RelayServerData clientData)
        {
            m_RelayServerData = serverData;
            m_RelayClientData = clientData;
        }

        /// <summary>
        /// This method will ensure that we only register a UDP driver. This forces the client to always go through the
        /// relay service. In a setup with client-hosted servers it will make sense to allow for IPC connections and
        /// UDP both, which is what invoking
        /// <see cref="DefaultDriverBuilder.RegisterClientDriver(World, ref NetworkDriverStore, NetDebug, ref RelayServerData)"/> will do.
        /// </summary>
        public void CreateClientDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
            var settings = DefaultDriverBuilder.GetNetworkSettings();
            settings.WithRelayParameters(ref m_RelayClientData);
            DefaultDriverBuilder.RegisterClientDriver(world, ref driverStore, netDebug, settings);
        }

        public void CreateServerDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
            DefaultDriverBuilder.RegisterServerDriver(world, ref driverStore, netDebug, ref m_RelayServerData);
            #else
            throw new System.NotSupportedException("It is not allowed to create a server NetworkDriver for WebGL build.");
            #endif
        }
    }
}
