using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(RotationSystemParents))]
public class RotationSpeedSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        var job = new RotationSpeedRotation
        {
            dt = Time.deltaTime
        };
        job.Schedule(this, 64).Complete();
    }

    [BurstCompile]
    private struct RotationSpeedRotation : IJobProcessComponentData<Rotation, RotationSpeed, Parent>
    {
        public float dt;

        public void Execute(ref Rotation rotation, [ReadOnly] ref RotationSpeed speed, [ReadOnly] ref Parent tp)
        {
            rotation.Value = math.mul(math.normalize(rotation.Value), quaternion.axisAngle(math.up(), speed.Value * dt));
        }
    }
}
