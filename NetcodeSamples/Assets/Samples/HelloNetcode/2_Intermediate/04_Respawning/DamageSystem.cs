using Unity.Entities;
using Unity.NetCode;

namespace Samples.HelloNetcode
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(HelloNetcodePredictedSystemGroup))]
    [UpdateAfter(typeof(ShootingSystem))]
    public partial struct DamageSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var healthFromEntity = SystemAPI.GetComponentLookup<Health>();
            foreach (var hit in SystemAPI.Query<RefRW<Hit>>())
            {
                if (!healthFromEntity.HasComponent(hit.ValueRO.Entity))
                {
                    continue;
                }

                var health = healthFromEntity[hit.ValueRO.Entity];
                health.CurrentHitPoints -= 20;
                healthFromEntity[hit.ValueRO.Entity] = health;
                hit.ValueRW.Entity = Entity.Null;
            }
        }
    }
}
