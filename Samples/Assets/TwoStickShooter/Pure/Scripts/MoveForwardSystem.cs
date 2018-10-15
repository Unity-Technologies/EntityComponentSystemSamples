using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace TwoStickPureExample
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

        ComponentGroup m_MoveForwardGroup;

        protected override void OnCreateManager()
        {
            m_MoveForwardGroup = GetComponentGroup(
                ComponentType.ReadOnly(typeof(MoveForward)),
                ComponentType.ReadOnly(typeof(MoveSpeed)),
                ComponentType.ReadOnly(typeof(Rotation)),
                typeof(Position));
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var moveForwardJob = new MoveForwardRotation
            {
                positions = m_MoveForwardGroup.GetComponentDataArray<Position>(),
                rotations = m_MoveForwardGroup.GetComponentDataArray<Rotation>(),
                moveSpeeds = m_MoveForwardGroup.GetComponentDataArray<MoveSpeed>(),
                dt = Time.deltaTime
            };
            var moveForwardJobHandle = moveForwardJob.Schedule(m_MoveForwardGroup.CalculateLength(), 64, inputDeps);
            return moveForwardJobHandle;
        }
    }
}
