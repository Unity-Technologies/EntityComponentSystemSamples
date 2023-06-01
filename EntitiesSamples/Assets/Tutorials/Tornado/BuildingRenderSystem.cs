using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Tutorials.Tornado
{
    /*
     * Updates the transforms of the bars.
     */
    public partial struct BuildingRenderSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new PointRenderJob
            {
                CurrentPoints = SystemAPI.GetSingleton<PointArrays>().current
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct PointRenderJob : IJobEntity
        {
            [ReadOnly] public NativeArray<float3> CurrentPoints;

            public void Execute(ref LocalToWorld ltw, in Bar bar, in BarThickness thickness)
            {
                var a = CurrentPoints[bar.pointA];
                var b = CurrentPoints[bar.pointB];

                var d = math.distance(a, b);

                var norm = (a - b) / d;

                var t = (a + b) / 2;
                var r = quaternion.LookRotationSafe(norm, norm.yzx);
                var s = new float3(new float2(thickness.Value), d);

                ltw.Value = float4x4.TRS(t, r, s);
            }
        }
    }
}
