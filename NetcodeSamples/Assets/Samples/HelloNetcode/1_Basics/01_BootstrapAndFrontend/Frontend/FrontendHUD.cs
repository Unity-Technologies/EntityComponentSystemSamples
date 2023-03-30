using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.NetCode;
using System.Collections.Generic;

namespace Samples.HelloNetcode
{
    public class FrontendHUD : MonoBehaviour
    {
        [SerializeField]
        UnityEngine.EventSystems.EventSystem m_EventSystem;

        public string ConnectionStatus
        {
            get { return m_ConnectionLabel.text; }
            set { m_ConnectionLabel.text = value; }
        }

        [SerializeField]
        UnityEngine.UI.Text m_ConnectionLabel;

        public void ReturnToFrontend()
        {
            var clientServerWorlds = new List<World>();
            foreach (var world in World.All)
            {
                if (world.IsClient() || world.IsServer())
                    clientServerWorlds.Add(world);
            }

            foreach (var world in clientServerWorlds)
                world.Dispose();

            if (string.IsNullOrEmpty(Frontend.OldFrontendWorldName))
                Frontend.OldFrontendWorldName = "DefaultWorld";
            ClientServerBootstrap.CreateLocalWorld(Frontend.OldFrontendWorldName);
            SceneManager.LoadScene("Frontend");
        }

        public void Start()
        {
            foreach (var world in World.All)
            {
                if (world.IsClient() && !world.IsThinClient())
                {
                    var sys = world.GetOrCreateSystemManaged<FrontendHUDSystem>();
                    sys.UIBehaviour = this;
                    var simGroup = world.GetExistingSystemManaged<SimulationSystemGroup>();
                    simGroup.AddSystemToUpdateList(sys);
                }
            }

            // We must always have an event system (DOTS-7177), but some scenes will already have one,
            // so we only enable ours if we can't find someone else's.
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>(false) == null)
                m_EventSystem.gameObject.SetActive(true);
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [DisableAutoCreation]
    public partial class FrontendHUDSystem : SystemBase
    {
        public FrontendHUD UIBehaviour;
        string m_PingText;

        protected override void OnUpdate()
        {
            CompleteDependency();
            if (!SystemAPI.TryGetSingletonEntity<NetworkStreamConnection>(out var connectionEntity))
            {
                UIBehaviour.ConnectionStatus = "Not connected!";
                m_PingText = default;
            }
            else
            {
                var connection = EntityManager.GetComponentData<NetworkStreamConnection>(connectionEntity);
                var address = SystemAPI.GetSingletonRW<NetworkStreamDriver>().ValueRO.GetRemoteEndPoint(connection).Address;
                if (EntityManager.HasComponent<NetworkId>(connectionEntity))
                {
                    if (string.IsNullOrEmpty(m_PingText) || UnityEngine.Time.frameCount % 30 == 0)
                    {
                        var networkSnapshotAck = EntityManager.GetComponentData<NetworkSnapshotAck>(connectionEntity);
                        m_PingText = networkSnapshotAck.EstimatedRTT > 0 ? $"{(int)networkSnapshotAck.EstimatedRTT}ms" : "Connected";
                    }

                    UIBehaviour.ConnectionStatus = $"{address} | {m_PingText}";
                }
                else
                    UIBehaviour.ConnectionStatus = $"{address} | Connecting";
            }
        }
    }
}
