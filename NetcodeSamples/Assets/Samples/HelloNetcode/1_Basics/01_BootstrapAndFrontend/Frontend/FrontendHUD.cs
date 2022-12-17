using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.NetCode;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Samples.HelloNetcode
{
    public class FrontendHUD : MonoBehaviour
    {
        public string ConnectionStatus
        {
            get { return m_ConnectionLabel.text; }
            set { m_ConnectionLabel.text = value; }
        }

        Label m_ConnectionLabel;
        Button m_MainMenuButton;

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

        void ReturnButtonClicked(ClickEvent evt)
        {
            ReturnToFrontend();
        }

        void OnEnable()
        {
            var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
            m_ConnectionLabel = rootVisualElement.Query<Label>("label");
            m_MainMenuButton = rootVisualElement.Query<Button>("button");
            m_MainMenuButton.RegisterCallback<ClickEvent>(ReturnButtonClicked);
        }

        void OnDisable()
        {
            m_MainMenuButton.UnregisterCallback<ClickEvent>(ReturnButtonClicked);
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
                if (EntityManager.HasComponent<NetworkIdComponent>(connectionEntity))
                {
                    if (string.IsNullOrEmpty(m_PingText) || UnityEngine.Time.frameCount % 30 == 0)
                    {
                        var networkSnapshotAck = EntityManager.GetComponentData<NetworkSnapshotAckComponent>(connectionEntity);
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
