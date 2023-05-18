using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
#if !UNITY_DISABLE_MANAGED_COMPONENTS
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial struct DespawnHealthBarSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (health, ui) in SystemAPI.Query<RefRO<Health>, HealthUI>())
            {
                if (health.ValueRO.CurrentHitPoints > 0)
                {
                    continue;
                }

                if (ui.HealthBar != null)
                {
                    Object.Destroy(ui.HealthBar.gameObject);
                    ui.HealthBar = null;
                    ui.HealthSlider = null;
                }
            }
        }
    }
#endif
}
