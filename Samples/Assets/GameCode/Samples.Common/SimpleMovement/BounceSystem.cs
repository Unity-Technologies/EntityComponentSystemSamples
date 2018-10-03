using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Samples.Common
{
    public class BounceSystem : JobComponentSystem
    {
#pragma warning disable 649
        struct BounceGroup
        {
            public ComponentDataArray<Position> positions;
            public ComponentDataArray<Bounce> bounce;
            public readonly int Length;
        } 

        [Inject] private BounceGroup m_BounceGroup;
#pragma warning restore 649    
        
        [BurstCompile]
        struct BouncePosition : IJobParallelFor
        {
            public ComponentDataArray<Position> positions;
            public ComponentDataArray<Bounce> bounce;
            public float dt;
        
            public void Execute(int i)
            {
                float t = bounce[i].t + (i*0.005f);
                float st = math.sin(t);
                float3 prevPosition = positions[i].Value;
                Bounce prevBounce = bounce[i];
                
                positions[i] = new Position
                {
                    Value = prevPosition + new float3( st*prevBounce.height.x, st*prevBounce.height.y, st*prevBounce.height.z )
                };

                bounce[i] = new Bounce
                {
                    t = prevBounce.t + (dt * prevBounce.speed),
                    height = prevBounce.height,
                    speed = prevBounce.speed
                };
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var bouncePositionJob = new BouncePosition();
            bouncePositionJob.positions = m_BounceGroup.positions;
            bouncePositionJob.bounce = m_BounceGroup.bounce;
            bouncePositionJob.dt = Time.deltaTime;
            return bouncePositionJob.Schedule(m_BounceGroup.Length, 64, inputDeps);
        } 
    }
}
