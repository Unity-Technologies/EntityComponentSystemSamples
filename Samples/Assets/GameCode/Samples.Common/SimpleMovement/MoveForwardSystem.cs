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
        struct MoveForwardRotation : IJobParallelFor
        {
            public ComponentDataArray<Position> positions;
            [ReadOnly] public ComponentDataArray<Rotation> rotations;
            [ReadOnly] public ComponentDataArray<MoveSpeed> moveSpeeds;
            public float dt;
        
            public void Execute(int i)
            {
                positions[i] = new Position
                {
                    Value = positions[i].Value + (dt * moveSpeeds[i].speed * math.forward(rotations[i].Value))
                };
            }
        }
        
        ComponentGroup m_MoveForwardRotationGroup;

        protected override void OnCreateManager()
        {
            m_MoveForwardRotationGroup = GetComponentGroup(
                ComponentType.ReadOnly(typeof(MoveForward)),
                ComponentType.ReadOnly(typeof(Rotation)),
                ComponentType.ReadOnly(typeof(MoveSpeed)),
                typeof(Position));
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var moveForwardRotationJob = new MoveForwardRotation
            {
                positions = m_MoveForwardRotationGroup.GetComponentDataArray<Position>(),
                rotations = m_MoveForwardRotationGroup.GetComponentDataArray<Rotation>(),
                moveSpeeds = m_MoveForwardRotationGroup.GetComponentDataArray<MoveSpeed>(),
                dt = Time.deltaTime
            };
            var moveForwardRotationJobHandle = moveForwardRotationJob.Schedule(m_MoveForwardRotationGroup.CalculateLength(), 64, inputDeps);
            return moveForwardRotationJobHandle;
        }
    }
}
