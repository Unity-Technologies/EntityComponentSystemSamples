using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Graphical.Splines
{
    [WorldSystemFilter(WorldSystemFilterFlags.Editor)]
    public partial struct SplineDebugRenderingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ExecuteSplines>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var spline in
                     SystemAPI.Query<RefRO<Spline>>())
            {
                ref var points = ref spline.ValueRO.Data.Value.Points;
                for (int i = 0; i < points.Length - 1; i += 1)
                {
                    Debug.DrawLine(points[i], points[i + 1], Color.magenta);
                }
            }
        }
    }
}
