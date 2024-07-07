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
                if (!healthFromEntity.TryGetComponent(hit.ValueRO.Victim, out var health))
                {
                    continue;
                }
                health.CurrentHitPoints -= 20;
                healthFromEntity[hit.ValueRO.Victim] = health;
                hit.ValueRW.Victim = Entity.Null;
            }
        }
    }
}
