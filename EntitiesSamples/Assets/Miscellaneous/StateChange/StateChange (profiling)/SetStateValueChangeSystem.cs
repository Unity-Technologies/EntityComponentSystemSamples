using Miscellaneous.Execute;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling.LowLevel.Unsafe;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Miscellaneous.StateChange
{
    public partial struct SetStateValueChangeSystem : ISystem
    {
        public struct EnableSingleton : IComponentData
        {
        }

        public struct State : IComponentData
        {
            public bool Enabled;
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Hit>();
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<EnableSingleton>();
            state.RequireForUpdate<StateChangeProfilerModule.FrameData>();
            state.RequireForUpdate<StateChangeProfiling>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var before = ProfilerUnsafeUtility.Timestamp;
            {
                var config = SystemAPI.GetSingleton<Config>();
                var job = new Job
                {
                    SqRadius = config.Radius * config.Radius,
                    Hit = SystemAPI.GetSingleton<Hit>().Value
                };
                job.ScheduleParallel();

                state.Dependency.Complete();
            }
            var after = ProfilerUnsafeUtility.Timestamp;

            var conversionRatio = ProfilerUnsafeUtility.TimestampToNanosecondsConversionRatio;
            var elapsed = (after - before) * conversionRatio.Numerator / conversionRatio.Denominator;
            SystemAPI.GetSingletonRW<StateChangeProfilerModule.FrameData>().ValueRW.SetStatePerf = elapsed;
        }

        [BurstCompile]
        partial struct Job : IJobEntity
        {
            public float SqRadius;
            public float3 Hit;

            void Execute(ref URPMaterialPropertyBaseColor color, ref State state, in LocalTransform transform)
            {
                bool inside = math.distancesq(transform.Position, Hit) < SqRadius;
                color.Value = (Vector4)(inside ? Color.red : Color.white);
                state.Enabled = inside;
            }
        }
    }
}
