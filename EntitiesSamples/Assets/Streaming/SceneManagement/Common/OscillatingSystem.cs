using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Streaming.SceneManagement.Common
{
    // OscillatingSystem will move each oscillating entity in sample
    public partial struct OscillatingSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var time = (float)SystemAPI.Time.ElapsedTime;
            foreach (var (transform, oscillating) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRO<Oscillating>>())
            {
                var amplitude = oscillating.ValueRO.Direction * oscillating.ValueRO.Amplitude;
                var wave = math.sin(2f * math.PI * oscillating.ValueRO.Frequency * time + oscillating.ValueRO.Offset);
                transform.ValueRW.Position = oscillating.ValueRO.Center + amplitude * wave;
            }
        }
    }
}
