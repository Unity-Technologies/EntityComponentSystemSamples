using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Samples.HelloNetcode
{
    [UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct HitTargetMoveSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<HitTarget>();
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (trans, hitTarget) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<HitTarget>>())
            {
                trans.ValueRW.Position.x = (float) math.sin(SystemAPI.Time.ElapsedTime * hitTarget.ValueRO.Speed) * hitTarget.ValueRO.MovingRange;
            }
        }
    }
}
