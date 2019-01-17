using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Samples.Common
{
    public class MoveForwardSystem : JobComponentSystem
    {
        [BurstCompile]
        struct MoveForwardRotation : IJobProcessComponentData<Position, Rotation, MoveSpeed>
        {
            public float dt;
        
            public void Execute(ref Position position, [ReadOnly] ref Rotation rotation, [ReadOnly] ref MoveSpeed speed)
            {
                position = new Position
                {
                    Value = position.Value + (dt * speed.speed * math.forward(rotation.Value))
                };
            }
        }
    
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var moveForwardRotationJob = new MoveForwardRotation
            {
                dt = Time.deltaTime
            };
            var moveForwardRotationJobHandle = moveForwardRotationJob.Schedule(this, inputDeps);
            return moveForwardRotationJobHandle;
        }
    }
}