using Miscellaneous.Execute;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling.LowLevel.Unsafe;
using Unity.Transforms;

namespace Miscellaneous.StateChange
{
    public partial struct RotateValueChangeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SetStateValueChangeSystem.EnableSingleton>();
            state.RequireForUpdate<StateChangeProfilerModule.FrameData>();
            state.RequireForUpdate<StateChangeProfiling>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var before = ProfilerUnsafeUtility.Timestamp;
            {
                new RotateValueJob
                {
                    Offset = quaternion.RotateY(SystemAPI.Time.DeltaTime * math.PI)
                }.ScheduleParallel();
                state.Dependency.Complete();
            }
            var after = ProfilerUnsafeUtility.Timestamp;

            var conversionRatio = ProfilerUnsafeUtility.TimestampToNanosecondsConversionRatio;
            var elapsed = (after - before) * conversionRatio.Numerator / conversionRatio.Denominator;
            SystemAPI.GetSingletonRW<StateChangeProfilerModule.FrameData>().ValueRW.RotatePerf = elapsed;
        }

        [BurstCompile]
        partial struct RotateValueJob : IJobEntity
        {
            public quaternion Offset;

            void Execute(ref LocalTransform transform, in SetStateValueChangeSystem.State state)
            {
                if (state.Enabled)
                {
                    transform = transform.Rotate(Offset);
                }
            }
        }
    }
}
