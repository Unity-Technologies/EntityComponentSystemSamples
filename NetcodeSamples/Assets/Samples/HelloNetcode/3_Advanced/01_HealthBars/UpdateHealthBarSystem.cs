using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

namespace Samples.HelloNetcode
{
#if !UNITY_DISABLE_MANAGED_COMPONENTS
    /// <summary>
    /// Update position and rotation of the health bar above players. This will make sure the health bar follow the character
    /// character and is always facing the main camera.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial struct UpdateHealthBarSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            if (Camera.main == null)
            {
                state.Enabled = false;
                return;
            }

            var mainCamera = Camera.main;

            foreach (var (ui, health, act, owner, ltw, entity) in SystemAPI.Query<HealthUI, RefRO<Health>, RefRO<AutoCommandTarget>, RefRO<GhostOwner>, RefRO<LocalToWorld>>().WithEntityAccess())
            {

                if (state.EntityManager.IsComponentEnabled<GhostOwnerIsLocal>(entity))
                {
                    // Move the UI on top of the local player, so that you can see.
                    var targetHealthBarPos = ltw.ValueRO.Position;
                    targetHealthBarPos.y += ui.PlayerHeightOffset;
                    var n = mainCamera.transform.position - ui.HealthBar.position;
                    targetHealthBarPos += (float3)(n.normalized * ui.PlayerTowardCameraOffset);
                    ui.HealthBar.SetPositionAndRotation(targetHealthBarPos, Quaternion.LookRotation(n));
                }
                else
                {
                    // Move the UI above the players head.
                    var targetHealthBarPos = ltw.ValueRO.Position;
                    targetHealthBarPos.y += ui.OpponentHeightOffset;
                    var n = mainCamera.transform.position - ui.HealthBar.position;
                    ui.HealthBar.SetPositionAndRotation(targetHealthBarPos, Quaternion.LookRotation(n));
                }

                var hpNormalized = math.saturate((float)health.ValueRO.CurrentHitPoints / health.ValueRO.MaximumHitPoints);
                var playerColor = NetworkIdDebugColorUtility.GetColor(owner.ValueRO.NetworkId);

                // Killed by server:
                if (act.ValueRO.Enabled)
                {
                    // Set to players color:
                    ui.HealthSlider.color = playerColor;
                }
                else
                {
                    // Set to 0 regardless of prediction, and change the background to an authoritative dead.
                    // Note that we only do this once AutoCommandTarget is set. Why? We're waiting for server confirmation.
                    hpNormalized = 0;
                    playerColor.a = 0.3f;
                    ui.HealthSlider.transform.parent.GetComponent<Image>().color = playerColor;
                }

                ui.HealthSlider.fillAmount = hpNormalized;
            }

        }
    }
#endif
}
