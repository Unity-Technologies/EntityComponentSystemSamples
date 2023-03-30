using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Miscellaneous.ClosestTarget
{
    [UpdateAfter(typeof(TargetingSystem))]
    [BurstCompile]
    public partial struct DebugLinesSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Execute.ClosestTarget>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (transform, target) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Target>>())
            {
                if (SystemAPI.Exists(target.ValueRO.Value))
                {
                    var targetTransform = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.Value);
                    Debug.DrawLine(transform.ValueRO.Position, targetTransform.Position);
                }
            }
        }
    }
}
