using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Miscellaneous.StateChangeValue
{
    public partial struct SetStateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Hit>();
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<Execute.StateChangeValue>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var hit = SystemAPI.GetSingleton<Hit>();
            if (hit.ChangedThisFrame)
            {
                var config = SystemAPI.GetSingleton<Config>();
                new SetStateJob
                {
                    SqRadius = config.Radius * config.Radius,
                    Hit = hit.Value
                }.ScheduleParallel();
            }
        }
    }

    [BurstCompile]
    partial struct SetStateJob : IJobEntity
    {
        public float SqRadius;
        public float3 Hit;

        void Execute(ref URPMaterialPropertyBaseColor color, ref Cube cube, in LocalTransform transform)
        {
            if (math.distancesq(transform.Position, Hit) < SqRadius)
            {
                // toggle spin state
                color.Value = (Vector4)(cube.IsSpinning ? Color.white : Color.red);
                cube.IsSpinning = !cube.IsSpinning;
            }
        }
    }
}
