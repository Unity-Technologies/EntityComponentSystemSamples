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
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

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
                }
            }
        }
    }
#endif
}
