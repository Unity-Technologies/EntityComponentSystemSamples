using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using UnityEngine;

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
        /// This method will ensure that we register different driver types based on the relay settings
        /// settings.
        /// <para>
        /// Mode          |  Relay Settings
        /// Client/Server |  Valid -> use relay to connect to local server
        ///                  Invalid -> use IPC to connect to local server
        /// Client        |  Always use relay. Expect data to be valid.
        /// <para>
        /// <para>
        /// For WebGL, websocket is always preferred for client in the Editor, to closely emulate the player behaviour.
        /// </para>
        /// </summary>
        public void CreateClientDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
            var settings = DefaultDriverBuilder.GetNetworkClientSettings();
            //if the relay data is not valid, connect via local ipc
            if(ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.ClientAndServer &&
               !m_RelayClientData.Endpoint.IsValid)
            {
                DefaultDriverBuilder.RegisterClientIpcDriver(world, ref driverStore, netDebug, settings);
            }
            else
            {
                settings.WithRelayParameters(ref m_RelayClientData);
#if !UNITY_WEBGL
                DefaultDriverBuilder.RegisterClientUdpDriver(world, ref driverStore, netDebug, settings);
#else
                DefaultDriverBuilder.RegisterClientWebSocketDriver(world, ref driverStore, netDebug, settings);
#endif
            }
        }

        public void CreateServerDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
            //The first driver is the IPC for internal client/server connection if necessary.
            var ipcSettings = DefaultDriverBuilder.GetNetworkServerSettings();
            DefaultDriverBuilder.RegisterServerIpcDriver(world, ref driverStore, netDebug, ipcSettings);
            var relaySettings = DefaultDriverBuilder.GetNetworkServerSettings();
            //The other driver (still the same port) is going to listen using relay for external conections
            relaySettings.WithRelayParameters(ref m_RelayServerData);
#if !UNITY_WEBGL
            DefaultDriverBuilder.RegisterServerUdpDriver(world, ref driverStore, netDebug, relaySettings);
#else
            DefaultDriverBuilder.RegisterServerWebSocketDriver(world, ref driverStore, netDebug, relaySettings);
#endif
        }
    }
}
