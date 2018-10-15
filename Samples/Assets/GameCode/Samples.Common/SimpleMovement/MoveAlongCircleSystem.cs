using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Samples.Common
{
    public class MoveAlongCircleSystem : JobComponentSystem
    {
#pragma warning disable 649
        struct MoveAlongCircleGroup
        {

            public ComponentDataArray<Position> positions;

            public ComponentDataArray<MoveAlongCircle> moveAlongCircles;
            [ReadOnly]
            public ComponentDataArray<MoveSpeed> moveSpeeds;
            public readonly int Length;
        }

        [Inject] private MoveAlongCircleGroup m_MoveAlongCircleGroup;
#pragma warning restore 649  
        
        [BurstCompile]
        struct MoveAlongCirclePosition : IJobParallelFor
        {
            public ComponentDataArray<Position> positions;
            public ComponentDataArray<MoveAlongCircle> moveAlongCircles;
            [ReadOnly]
            public ComponentDataArray<MoveSpeed> moveSpeeds;
            public float dt;
        
            public void Execute(int i)
            {
                float t = moveAlongCircles[i].t + (dt * moveSpeeds[i].speed);
                float offsetT = t + (0.01f * i);
                float x = moveAlongCircles[i].center.x + (math.cos(offsetT) * moveAlongCircles[i].radius);
                float y = moveAlongCircles[i].center.y;
                float z = moveAlongCircles[i].center.z + (math.sin(offsetT) * moveAlongCircles[i].radius);

                moveAlongCircles[i] = new MoveAlongCircle
                {
                    t = t,
                    center = moveAlongCircles[i].center,
                    radius = moveAlongCircles[i].radius
                };
                
                positions[i] = new Position
                {
                    Value = new float3(x,y,z)
                };
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var moveAlongCirclePositionJob = new MoveAlongCirclePosition();
            moveAlongCirclePositionJob.positions = m_MoveAlongCircleGroup.positions;
            moveAlongCirclePositionJob.moveAlongCircles = m_MoveAlongCircleGroup.moveAlongCircles;
            moveAlongCirclePositionJob.moveSpeeds = m_MoveAlongCircleGroup.moveSpeeds;
            moveAlongCirclePositionJob.dt = Time.deltaTime;
            return moveAlongCirclePositionJob.Schedule(m_MoveAlongCircleGroup.Length, 64, inputDeps);
        } 
    }
}
