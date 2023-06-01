using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Streaming.SceneManagement.CrossSectionReferences
{
    public partial struct VelocitySystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var time = SystemAPI.Time.DeltaTime;
            foreach (var (transform, velocity) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRO<Velocity>>())
            {
                transform.ValueRW.Position += time * velocity.ValueRO.Value;
            }
        }
    }
}
