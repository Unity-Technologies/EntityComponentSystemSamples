using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;

namespace Samples.Common
{
    public class GravitySystem : JobComponentSystem
    {
        [BurstCompile]
        [RequireComponentTag(typeof(Gravity))]
        struct GravityPosition : IJobProcessComponentData<Position>
        {
            public void Execute(ref Position position)
            {
                position.Value = position.Value - new float3(0.0f, 9.8f, 0.0f);
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new GravityPosition().Schedule(this, inputDeps);
        }
    }
}
