using Miscellaneous.Execute;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling.LowLevel.Unsafe;
using Unity.Transforms;

namespace Miscellaneous.StateChange
{
    public partial struct RotateStructuralChangeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SetStateStructuralChangeSystem.EnableSingleton>();
            state.RequireForUpdate<StateChangeProfiling>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var before = ProfilerUnsafeUtility.Timestamp;
            {
                new RotateStructuralJob
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
    }

    [WithAll(typeof(SetStateStructuralChangeSystem.StateEnabled))]
    [BurstCompile]
    partial struct RotateStructuralJob : IJobEntity
    {
        public quaternion Offset;

        void Execute(ref LocalTransform transform)
        {
            transform = transform.Rotate(Offset);
        }
    }
}
