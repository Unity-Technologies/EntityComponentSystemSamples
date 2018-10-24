using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Samples.Common
{
    public class RotationSpeedSystem : JobComponentSystem
    {
        [BurstCompile]
        struct RotationSpeedRotation : IJobProcessComponentData<Rotation, RotationSpeed>
        {
            public float dt;

            public void Execute(ref Rotation rotation, [ReadOnly]ref RotationSpeed speed)
            {
                rotation.Value = math.mul(math.normalize(rotation.Value), quaternion.AxisAngle(math.up(), speed.Value * dt));
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new RotationSpeedRotation() { dt = Time.deltaTime };
            return job.Schedule(this, inputDeps);
        }
    }
}
