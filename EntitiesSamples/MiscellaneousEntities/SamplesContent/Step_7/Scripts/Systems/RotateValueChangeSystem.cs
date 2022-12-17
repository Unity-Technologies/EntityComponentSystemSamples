using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling.LowLevel.Unsafe;
using Unity.Transforms;

namespace StateChange
{
    [BurstCompile]
    public partial struct RotateValueChangeSystem : ISystem
    {
        [BurstCompile]
        partial struct Job : IJobEntity
        {
            public quaternion Offset;

            void Execute(TransformAspect transform, in SetStateValueChangeSystem.State state)
            {
                if (state.Enabled)
                {
                    transform.RotateLocal(Offset);
                }
            }
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SetStateValueChangeSystem.EnableSingleton>();
            state.RequireForUpdate<StateChangeProfilerModule.FrameData>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var before = ProfilerUnsafeUtility.Timestamp;
            {
                var job = new Job
                {
                    Offset = quaternion.RotateY(SystemAPI.Time.DeltaTime * math.PI)
                };
                job.ScheduleParallel();
                state.Dependency.Complete();
            }
            var after = ProfilerUnsafeUtility.Timestamp;

            var conversionRatio = ProfilerUnsafeUtility.TimestampToNanosecondsConversionRatio;
            var elapsed = (after - before) * conversionRatio.Numerator / conversionRatio.Denominator;
            SystemAPI.GetSingletonRW<StateChangeProfilerModule.FrameData>().ValueRW.RotatePerf = elapsed;
        }
    }
}