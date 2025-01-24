using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Scenes;
using Unity.Transforms;

namespace Tutorials.Tornado
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SceneSystemGroup))]
    public partial struct TornadoSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();
            var entities = state.EntityManager.Instantiate(config.ParticlePrefab, 1000, Allocator.Temp);

            var random = Random.CreateFromIndex(1234);

            foreach (var entity in entities)
            {
                var particle = SystemAPI.GetComponentRW<Particle>(entity);
                var transform = SystemAPI.GetComponentRW<LocalTransform>(entity);
                var color = SystemAPI.GetComponentRW<URPMaterialPropertyBaseColor>(entity);

                transform.ValueRW.Position = new float3(random.NextFloat(-50f, 50f), random.NextFloat(0f, 50f),
                    random.NextFloat(-50f, 50f));
                transform.ValueRW.Scale = random.NextFloat(.2f, .7f);
                particle.ValueRW.radiusMult = random.NextFloat();
                color.ValueRW.Value = new float4(new float3(random.NextFloat(.3f, .7f)), 1f);
            }

            state.Enabled = false;
        }
    }
}
