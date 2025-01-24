#if UNITY_EDITOR
using Unity.Entities;
using Unity.Profiling;
using Unity.Profiling.Editor;

namespace HelloCube.StateChange
{
    [ProfilerModuleMetadata("StateChange")]
    public class StateChangeProfilerModule : ProfilerModule
    {
        public struct FrameData : IComponentData
        {
            public long SpinPerf;
            public long SetStatePerf;
        }

        static readonly string s_SpinPerfCounterLabel = "Spin System";
        static readonly string s_SetStatePerfCounterLabel = "SetState System";

        static readonly ProfilerCounterValue<long> s_SpinPerfCounterValue = new(
            ProfilerCategory.Scripts,
            s_SpinPerfCounterLabel,
            ProfilerMarkerDataUnit.TimeNanoseconds,
            ProfilerCounterOptions.FlushOnEndOfFrame);

        static readonly ProfilerCounterValue<long> s_SetStatePerfCounterValue = new(
            ProfilerCategory.Scripts,
            s_SetStatePerfCounterLabel,
            ProfilerMarkerDataUnit.TimeNanoseconds,
            ProfilerCounterOptions.FlushOnEndOfFrame);

        static readonly ProfilerCounterDescriptor[] k_ChartCounters =
        {
            new(s_SpinPerfCounterLabel, ProfilerCategory.Scripts),
            new(s_SetStatePerfCounterLabel, ProfilerCategory.Scripts),
        };

        internal static long SpinPerf
        {
            set => s_SpinPerfCounterValue.Value = value;
        }

        internal static long UpdatePerf
        {
            set => s_SetStatePerfCounterValue.Value = value;
        }

        public StateChangeProfilerModule() : base(k_ChartCounters, ProfilerModuleChartType.StackedTimeArea)
        {
        }
    }
}

#endif
