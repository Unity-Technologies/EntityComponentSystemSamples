using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Samples.Common
{
    public class RotationAccelerationSystem : JobComponentSystem
    {
        [BurstCompile]
        struct RotationSpeedAcceleration : IJobProcessComponentData<RotationSpeed, RotationAcceleration>
        {
            public float dt;
        
            public void Execute(ref RotationSpeed speed, [ReadOnly]ref RotationAcceleration acceleration)
            {
                speed.Value = math.max(0.0f, speed.Value + (acceleration.speed * dt));
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var rotationSpeedAccelerationJob = new RotationSpeedAcceleration { dt = Time.deltaTime };
            return rotationSpeedAccelerationJob.Schedule(this, inputDeps);
        } 
    }
}