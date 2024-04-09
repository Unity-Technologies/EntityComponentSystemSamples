using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Graphical.Splines
{
    [UpdateAfter(typeof(SnakeSpawnSystem))]
    public partial struct SnakeUpdateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SnakeSettings>();
            state.RequireForUpdate<ExecuteSplines>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var transformLookup =  SystemAPI.GetComponentLookup<LocalTransform>();
            var splineFromEntity = SystemAPI.GetComponentLookup<Spline>(true);

            new SnakeJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                TransformLookup = transformLookup,
                SplineLookup = splineFromEntity
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct SnakeJob: IJobEntity
    {
        public float DeltaTime;

        [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> TransformLookup;

        [ReadOnly] public ComponentLookup<Spline> SplineLookup;

        void Execute(ref Snake snake, in DynamicBuffer<SnakePart> snakeParts)
        {
            var splineData = SplineLookup[snake.SplineEntity].Data;
            ref var points = ref splineData.Value.Points;
            ref var distance = ref splineData.Value.Distance;
            var offset = snake.Offset + DeltaTime * snake.Speed;
            snake.Offset = offset;

            var maxDist = distance[distance.Length - 1];

            for (int i = 0; i < snakeParts.Length; i += 1)
            {
                offset = (offset % maxDist + maxDist) % maxDist;

                int idx = distance.LowerBound(offset);
                var a = distance[idx];
                var b = distance[idx + 1];

                float t = (offset - a) / (b - a);
                float3 pos = math.lerp(points[idx], points[idx + 1], t);

                var transform = TransformLookup[snakeParts[i].Value];
                transform.Position = pos + snake.Anchor;
                TransformLookup[snakeParts[i].Value] = transform;

                offset += snake.Spacing;
            }
        }
    }
}
