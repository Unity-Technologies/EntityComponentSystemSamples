using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace ClosestTarget
{
    [UpdateAfter(typeof(TargetingSystem))]
    [BurstCompile]
    public partial struct DebugLinesSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (transform, target) in SystemAPI.Query<TransformAspect, RefRO<Target>>())
            {
                if (SystemAPI.Exists(target.ValueRO.Value))
                {
                    var targetTransform = SystemAPI.GetAspectRO<TransformAspect>(target.ValueRO.Value);

                    var src = transform.LocalPosition;
                    var dst = targetTransform.LocalPosition;
                    Debug.DrawLine(src, dst);
                }
            }
        }
    }
}