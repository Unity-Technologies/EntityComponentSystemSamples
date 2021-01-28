using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using static Unity.Mathematics.math;

public class StressTestAnimationSystem : SystemBase
{
    void AnimateColors(SimulationMode.Mode mode)
    {
        Entities.WithAll<ColorAnimated>()
            .ForEach((ref MaterialColor color, in SpawnIndex index) =>
            {
                float indexAdd = 0.123f * (float)index.Value;
                float time = mode.time * 5.0f;

                var v = new float3(math.cos(time + indexAdd), math.sin(time + indexAdd), 0.0f);
                color.Value = float4(v * 0.4f + 0.5f, 0);
            }).ScheduleParallel();

        // Color animation of LOD children, they don't have SpawnIndex
        Entities.WithAll<ColorAnimated>()
            .WithNone<SpawnIndex>()
            .ForEach((ref MaterialColor color) =>
            {
                float indexAdd = 4.56f;
                float time = mode.time * 5.0f;

                var v = new float3(math.cos(time + indexAdd), math.sin(time + indexAdd), 0.0f);
                color.Value = float4(v * 0.4f + 0.5f, 0);
            }).ScheduleParallel();
    }

    void AnimatePositions(SimulationMode.Mode mode)
    {
        Entities.WithAll<PositionAnimated>()
            .ForEach((ref Translation translation, in SpawnIndex index) =>
            {
                float indexAdd = 0.00123f * (float)index.Value;
                float time = mode.time * 2.0f;

                int y = index.Value / index.Height;
                int x = index.Value - (y * index.Height);
                var pos = new float4(x, math.sin(time + indexAdd) * 4.0f, y, 1);

                translation.Value = pos.xyz;
            }).ScheduleParallel();
    }

    protected override void OnUpdate()
    {
        var mode = SimulationMode.getCurrentMode();

        switch (mode.type)
        {
            case SimulationMode.ModeType.Color:
                AnimateColors(mode);
                break;
            case SimulationMode.ModeType.Position:
                AnimatePositions(mode);
                break;
            case SimulationMode.ModeType.PositionAndColor:
                AnimateColors(mode);
                AnimatePositions(mode);
                break;
            default:
                break;        
        }
    }
}
