using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Samples.Common
{
    public class LocalRotationSpeedSystem : JobComponentSystem
    {
        struct LocalRotationSpeedGroup
        {
            public ComponentDataArray<LocalRotation> rotations;
            [ReadOnly] public ComponentDataArray<LocalRotationSpeed> rotationSpeeds;
            public readonly int Length;
        }

        [Inject] private LocalRotationSpeedGroup m_LocalRotationSpeedGroup;
    
        [BurstCompile]
        struct LocalRotationSpeedLocalRotation : IJobParallelFor
        {
            public ComponentDataArray<LocalRotation> rotations;
            [ReadOnly] public ComponentDataArray<LocalRotationSpeed> rotationSpeeds;
            public float dt;
        
            public void Execute(int i)
            {
                var speed = rotationSpeeds[i].Value;
                if (speed > 0.0f)
                {
                    rotations[i] = new LocalRotation
                    {
                        Value = math.mul(math.normalize(rotations[i].Value), quaternion.axisAngle(math.up(),speed*dt))
                    };
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var rotationSpeedLocalRotationJob = new LocalRotationSpeedLocalRotation
            {
                rotations = m_LocalRotationSpeedGroup.rotations,
                rotationSpeeds = m_LocalRotationSpeedGroup.rotationSpeeds,
                dt = Time.deltaTime
            };
            return rotationSpeedLocalRotationJob.Schedule(m_LocalRotationSpeedGroup.Length, 64, inputDeps);
        } 
    }
}
