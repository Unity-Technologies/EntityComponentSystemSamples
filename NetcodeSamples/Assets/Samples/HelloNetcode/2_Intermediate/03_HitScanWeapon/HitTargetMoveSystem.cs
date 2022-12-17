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

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var timeDeltaTime = SystemAPI.Time.DeltaTime;
#if !ENABLE_TRANSFORM_V1
            foreach (var (trans, hitTarget) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<HitTarget>>())
#else
            foreach (var (trans, hitTarget) in SystemAPI.Query<RefRW<Translation>, RefRW<HitTarget>>())
#endif
            {
                var deltaMove = timeDeltaTime * hitTarget.ValueRW.Speed;
                hitTarget.ValueRW.Moved += deltaMove;
#if !ENABLE_TRANSFORM_V1
                trans.ValueRW.Position.x += deltaMove;
#else
                trans.ValueRW.Value.x += deltaMove;
#endif
                if (math.abs(hitTarget.ValueRW.Moved) > hitTarget.ValueRW.MovingRange)
                {
                    hitTarget.ValueRW.Speed = -hitTarget.ValueRW.Speed;
                }
            }
        }
    }
}
