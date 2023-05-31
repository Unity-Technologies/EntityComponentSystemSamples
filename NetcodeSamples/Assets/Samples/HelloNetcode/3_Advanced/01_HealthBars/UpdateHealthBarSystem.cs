using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

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

            foreach (var (ui, health, localTransform) in SystemAPI.Query<HealthUI, RefRO<Health>, RefRO<LocalTransform>>())
            {
                if (health.ValueRO.CurrentHitPoints <= 0)
                {
                    continue;
                }
                ui.HealthBar.position = localTransform.ValueRO.Position + ui.Offset;
                var n = mainCamera.transform.position - ui.HealthBar.position;
                ui.HealthBar.rotation = Quaternion.LookRotation(n);
                ui.HealthSlider.fillAmount = health.ValueRO.CurrentHitPoints / health.ValueRO.MaximumHitPoints;
            }

        }
    }
#endif
}
