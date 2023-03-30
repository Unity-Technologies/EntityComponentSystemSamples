using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;

namespace Samples.HelloNetcode
{
    public class LagUI : MonoBehaviour
    {
        public static bool EnableLagCompensation = true;

        public Toggle LagToggle;

        void Update()
        {
            EnableLagCompensation = LagToggle.isOn;
        }
    }

    public struct ToggleLagCompensationRequest : IRpcCommand
    {
        public bool Enable;
        public Entity Player;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct LagUISystem : ISystem
    {
        bool m_PrevEnabled;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableHitScanWeapons>();
            state.RequireForUpdate<NetworkId>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (m_PrevEnabled == LagUI.EnableLagCompensation
                || !SystemAPI.TryGetSingletonEntity<CharacterControllerPlayerInput>(out var player))
            {
                return;
            }
            m_PrevEnabled = LagUI.EnableLagCompensation;
            var ent = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(ent, new ToggleLagCompensationRequest { Enable = m_PrevEnabled, Player = player });
            state.EntityManager.AddComponentData(ent, default(SendRpcCommandRequest));
        }
    }

    public struct LagCompensationEnabled : IComponentData
    {
    }

    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
    public partial struct LagUIControlSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            // Process requests to toggle lag compensation
            var cmdBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (toggleRO, entity) in SystemAPI.Query<RefRO<ToggleLagCompensationRequest>>().WithEntityAccess())
            {
                // Find the correct control entity
                var toggle= toggleRO.ValueRO;
                switch (toggle.Enable)
                {
                    case false when state.EntityManager.HasComponent<LagCompensationEnabled>(toggle.Player):
                        cmdBuffer.RemoveComponent<LagCompensationEnabled>(toggle.Player);
                        break;
                    case true when !state.EntityManager.HasComponent<LagCompensationEnabled>(toggle.Player):
                        cmdBuffer.AddComponent<LagCompensationEnabled>(toggle.Player);
                        break;
                }
                cmdBuffer.DestroyEntity(entity);
            }
            cmdBuffer.Playback(state.EntityManager);
        }
    }
}
