using Unity.Entities;
using Unity.Profiling;
using Unity.Profiling.Editor;

[ProfilerModuleMetadata("StateChange")]
public class StateChangeProfilerModule : ProfilerModule
{
    public struct FrameData : IComponentData
    {
        public long RotatePerf;
        public long SetStatePerf;
    }

    static readonly string s_RotatePerfCounterLabel = "Rotate System";
    static readonly string s_SetStatePerfCounterLabel = "SetState System";

    static readonly ProfilerCounterValue<long> s_RotatePerfCounterValue = new(
        ProfilerCategory.Scripts,
        s_RotatePerfCounterLabel,
        ProfilerMarkerDataUnit.TimeNanoseconds,
        ProfilerCounterOptions.FlushOnEndOfFrame);

    static readonly ProfilerCounterValue<long> s_SetStatePerfCounterValue = new(
        ProfilerCategory.Scripts,
        s_SetStatePerfCounterLabel,
        ProfilerMarkerDataUnit.TimeNanoseconds,
        ProfilerCounterOptions.FlushOnEndOfFrame);

    static readonly ProfilerCounterDescriptor[] k_ChartCounters =
    {
        new(s_RotatePerfCounterLabel, ProfilerCategory.Scripts),
        new(s_SetStatePerfCounterLabel, ProfilerCategory.Scripts),
    };

    internal static long SpinPerf
    {
        set => s_RotatePerfCounterValue.Value = value;
    }

    internal static long UpdatePerf
    {
        set => s_SetStatePerfCounterValue.Value = value;
    }

    public StateChangeProfilerModule() : base(k_ChartCounters, ProfilerModuleChartType.StackedTimeArea)
    {
    }
}