using Unity.Collections;
using Unity.Entities;
using Unity.NetCode.HostMigration;
using Unity.Networking.Transport;
using UnityEngine;

namespace Unity.NetCode.Samples.Common
{
    public static class HostMigrationHelper
    {
        /// <summary>
        /// Optional helper method to assist with the operations needed during a host migration process. <see cref="SetHostMigrationData"/>
        /// can also be called directly with a server world which has been manually set up to resume hosting
        /// with a given host migration data.
        ///
        /// Takes the given host migration data and starts the host migration process. This starts loading
        /// the entity scenes the host previously had loaded (if any). The <see cref="NetworkDriverStore"/> and <see cref="NetworkDriver"/> in the
        /// new server world will be created appropriately, the driver constructor will need to be capable of
        /// setting up the relay connection with the given constructor. The local client world will switch from relay to
        /// local IPC connection to the server world.
        /// </summary>
        /// <param name="driverConstructor">The network driver constructor registered in the new server world and also in the client world.</param>
        /// <param name="migrationData">The data blob containing host migration data, deployed to the new server world.</param>
        /// <returns>Returns false if there was any immediate failure when starting up the new server</returns>
        public static bool MigrateDataToNewServerWorld(INetworkStreamDriverConstructor driverConstructor, ref NativeArray<byte> migrationData)
        {
            var oldConstructor = NetworkStreamReceiveSystem.DriverConstructor;
            NetworkStreamReceiveSystem.DriverConstructor = driverConstructor;
            var serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
            NetworkStreamReceiveSystem.DriverConstructor = oldConstructor;

            if (migrationData.Length == 0)
                Debug.LogWarning($"No host migration data given during host migration, no data will be deployed.");
            else
                HostMigrationData.Set(migrationData, serverWorld);

            using var serverDriverQuery = serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            var serverDriver = serverDriverQuery.GetSingletonRW<NetworkStreamDriver>();
            if (!serverDriver.ValueRW.Listen(NetworkEndpoint.AnyIpv4))
            {
                Debug.LogError($"NetworkStreamDriver.Listen() failed");
                return false;
            }

            var ipcPort = serverDriver.ValueRW.GetLocalEndPoint(serverDriver.ValueRW.DriverStore.FirstDriver).Port;

            // The client driver needs to be recreated, and then directly connected to new server world via IPC
            return ConfigureClientAndConnect(ClientServerBootstrap.ClientWorld, driverConstructor, NetworkEndpoint.LoopbackIpv4.WithPort(ipcPort));
        }

        /// <summary>
        /// Optional helper method to create the client driver with the given driver constructor and connect to the endpoint.
        /// The NetworkDriverStore will be recreated as the client can be switching from a local IPC connection to relay
        /// connection or reversed, the relay data can be set at driver creation time.
        /// </summary>
        /// <param name="clientWorld">The client world which needs to be configured.</param>
        /// <param name="driverConstructor">The network driver constructor used for creating a new network driver in the client world.</param>
        /// <param name="serverEndpoint">The network endpoint the client will connect to after configuring the network driver.</param>
        /// <returns>Returns true if the connect call succeeds</returns>
        public static bool ConfigureClientAndConnect(World clientWorld, INetworkStreamDriverConstructor driverConstructor, NetworkEndpoint serverEndpoint)
        {
            if (clientWorld == null || !clientWorld.IsCreated)
            {
                Debug.LogError("HostMigration.ConfigureClientAndConnect: Invalid client world provided");
                return false;
            }

            using var clientNetDebugQuery = clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetDebug>());
            var clientNetDebug = clientNetDebugQuery.GetSingleton<NetDebug>();
            var clientDriverStore = new NetworkDriverStore();
            driverConstructor.CreateClientDriver(clientWorld, ref clientDriverStore, clientNetDebug);
            using var clientDriverQuery = clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            var clientDriver = clientDriverQuery.GetSingleton<NetworkStreamDriver>();
            clientDriver.ResetDriverStore(clientWorld.Unmanaged, ref clientDriverStore);

            var connectionEntity = clientDriver.Connect(clientWorld.EntityManager, serverEndpoint);
            if (connectionEntity == Entity.Null)
                return false;
            return true;
        }
    }
}
