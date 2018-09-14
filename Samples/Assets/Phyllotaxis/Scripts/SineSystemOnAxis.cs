using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class SineSystemOnAxis : JobComponentSystem
{
    public static SineSystemOnAxis ssoa;
    private readonly float3 oriP = new float3(0, 0, 0);
    private int sign = -1;
    private float temp;

    protected override void OnCreateManager()
    {
        ssoa = this;
        Enabled = false;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (temp % 100000 == 0)
            sign = sign * -1;
        var job = new SineSystem
        {
            originPoint = oriP,
            temp = temp,
            sign = sign,
            dt = Time.deltaTime,
            Const = Bootstrap.Settings.lerpFact
        };
        temp = temp + Time.deltaTime;
        return job.Schedule(this, inputDeps);
    }

    [BurstCompile]
    private struct SineSystem : IJobProcessComponentData<Position, RotationSpeed>
    {
        [ReadOnly] public float3 originPoint;
        [ReadOnly] public float temp;
        [ReadOnly] public float dt;
        [ReadOnly] public int sign;
        [ReadOnly] public float Const;
        private float3 point;

        public void Execute(ref Position position, [ReadOnly] ref RotationSpeed speed)
        {
            point = position.Value;
            var distanceFromCenter = math.distance(position.Value, originPoint);
            point.y = point.y + math.sin(distanceFromCenter + temp) * 25;

            position.Value = math.lerp(position.Value, point, dt * Const);
        }
    }
}
