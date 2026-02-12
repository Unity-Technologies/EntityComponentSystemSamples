using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.NetCode.HostMigration;
using Unity.Networking.Transport.Relay;
using UnityEngine;
using UnityEngine.UI;

namespace Samples.HelloNetcode
{
    public class HostMigrationHUD : MonoBehaviour
    {
        public Text StatsText;

#if !UNITY_SERVER
        void Start()
        {
            if (ClientServerBootstrap.ServerWorld != null)
            {
                var hudSystem = ClientServerBootstrap.ServerWorld.GetOrCreateSystemManaged<ServerHostMigrationHUDSystem>();
                hudSystem.StatsText = StatsText;
            }
            else if (ClientServerBootstrap.ClientWorld != null)
            {
                var hudSystem = ClientServerBootstrap.ClientWorld.GetOrCreateSystemManaged<ClientHostMigrationHUDSystem>();
                hudSystem.StatsText = StatsText;
            }
        }

        /// <summary>
        /// Set up the component for tracking the relay connection status during the host migration, the HUD UI then prints the information
        /// </summary>
        public static Entity SetWaitForRelayConnection(WaitForRelayConnection waitComponent)
        {
            using var relayEntityQuery = ClientServerBootstrap.ClientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<WaitForRelayConnection>());
            var relayEntity = Entity.Null;
            // There could already be a WaitForRelayConnection if we got a host migration event but the host failed and another one was picked
            if (!relayEntityQuery.IsEmptyIgnoreFilter)
                relayEntity = relayEntityQuery.ToEntityArray(Allocator.Temp)[0];
            else
                relayEntity = ClientServerBootstrap.ClientWorld.EntityManager.CreateEntity(ComponentType.ReadOnly<WaitForRelayConnection>());
            ClientServerBootstrap.ClientWorld.EntityManager.AddComponentData(relayEntity, waitComponent);
            return relayEntity;
        }
    }

    public struct WaitForRelayConnection : IComponentData
    {
        public bool WaitForJoinCode;
        public bool WaitForHostSetup;
        public bool IsHostMigration;
        public float StartTime;
        public FixedString32Bytes OldJoinCode;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class ClientHostMigrationHUDSystem : SystemBase
    {
        public Text StatsText;
        EntityQuery m_RelayQuery;
        public FixedString32Bytes RelayJoinCode;

        protected override void OnCreate()
        {
            m_RelayQuery = GetEntityQuery(ComponentType.ReadOnly<WaitForRelayConnection>());
        }

        protected override void OnUpdate()
        {
            if (StatsText != null)
            {
                var prefix = "<color=#008000ff>[Client]</color>";
                if (!m_RelayQuery.IsEmptyIgnoreFilter)
                {
                    var waitData = m_RelayQuery.GetSingleton<WaitForRelayConnection>();
                    if (waitData.IsHostMigration)
                        StatsText.text = $"{prefix} Host migration in progress: ";
                    else
                        StatsText.text = $"{prefix} ";
                    var connectionTime = UnityEngine.Time.realtimeSinceStartup - waitData.StartTime;

                    // If we're waiting for relay but don't have a join code yet, we're still waiting for the lobby to send it to us
                    if (waitData.WaitForJoinCode)
                    {
                        StatsText.text += $"Waiting for new join code ({connectionTime:F2} s)";
                        if (waitData.OldJoinCode == RelayJoinCode)
                            return;
                        waitData.WaitForJoinCode = false;
                        waitData.StartTime = UnityEngine.Time.realtimeSinceStartup;
                        World.EntityManager.SetComponentData(m_RelayQuery.GetSingletonEntity(), waitData);
                        Debug.Log($"{prefix}[HostMigration] New join code received ({connectionTime:F2} s)");
                        return;
                    }

                    if (waitData.WaitForHostSetup)
                    {
                        StatsText.text += $"Waiting for host migration data ({connectionTime:F2} s)";
                        return;
                    }

                    // TODO: Seems this catches the old relay connection when a host migration happens, so immediately sees Established
                    var relayEntity = m_RelayQuery.GetSingletonEntity();
                    CheckRelayStatus(StatsText, World, relayEntity, prefix, connectionTime);
                }
                else
                {
                    StatsText.text = $"{prefix} Ready.";
                }
            }
        }

        internal static void CheckRelayStatus(Text statusText, World world, Entity relayEntity, string prefix, double connectionTime)
        {
            var relayConnectionStatus = GetRelayConnectionStatus(world);
            switch (relayConnectionStatus)
            {
                case RelayConnectionStatus.Established:
                    statusText.text += "Relay connection established";
                    Debug.Log($"{prefix}[HostMigration] Relay connection established ({connectionTime:F2} s)");
                    world.EntityManager.DestroyEntity(relayEntity);
                    break;
                case RelayConnectionStatus.NotEstablished:
                    statusText.text += $"Connecting to relay server ({connectionTime:F2} s)";
                    break;
                case RelayConnectionStatus.AllocationInvalid:
                    statusText.text += "Relay connection failed; allocation is invalid";
                    break;
                // During client migration from old host to the new there will be some time where it's not connected to anything and waiting for the new join code
                case RelayConnectionStatus.NotUsingRelay:
                    break;
                default:
                    statusText.text += "Unexpected Relay connection status";
                    break;
            }
        }

        static RelayConnectionStatus GetRelayConnectionStatus(World world)
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
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class ServerHostMigrationHUDSystem : SystemBase
    {
        public Text StatsText;
        EntityQuery m_RelayQuery;

        protected override void OnCreate()
        {
            RequireForUpdate<HostMigrationStats>();
            m_RelayQuery = GetEntityQuery(ComponentType.ReadOnly<WaitForRelayConnection>());
        }

        protected override void OnUpdate()
        {
            var stats = SystemAPI.GetSingleton<HostMigrationStats>();
            if (StatsText != null)
            {
                var prefix = "<color=#ff0000ff>[Server]</color>";
                if (!m_RelayQuery.IsEmptyIgnoreFilter)
                {
                    var waitData = m_RelayQuery.GetSingleton<WaitForRelayConnection>();
                    if (waitData.IsHostMigration)
                        StatsText.text = $"{prefix} Host migration in progress: ";
                    else
                        StatsText.text = $"{prefix} Starting: ";

                    var connectionTime = UnityEngine.Time.realtimeSinceStartup - waitData.StartTime;
                    var relayEntity = m_RelayQuery.GetSingletonEntity();
                    ClientHostMigrationHUDSystem.CheckRelayStatus(StatsText, World, relayEntity, prefix, connectionTime);
                }
                else
                {
                    StatsText.text = $"{prefix} Ghost Count: {stats.GhostCount} Prefab Count: {stats.PrefabCount} Update Size: {stats.UpdateSize} Total Update Size: {stats.TotalUpdateSize}";
                }
            }
        }
#endif
    }
}
