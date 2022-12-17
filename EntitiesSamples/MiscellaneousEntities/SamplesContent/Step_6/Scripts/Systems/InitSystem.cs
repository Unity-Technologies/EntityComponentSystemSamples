using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace StateMachine
{
    [BurstCompile]
    public partial struct InitSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var config = SystemAPI.GetSingleton<Config>();
            state.EntityManager.Instantiate(config.Prefab, (int)(config.Size * config.Size), Allocator.Temp);

            var center = (config.Size - 1) / 2f;
            int i = 0;
            foreach (var (trans, spinnerEnabled) in SystemAPI.Query<RefRW<LocalTransform>, EnabledRefRW<Spinner>>().WithAll<Cube>())
            {
                spinnerEnabled.ValueRW = false;
                trans.ValueRW.Scale = 1;
                trans.ValueRW.Position.x = (i % config.Size - center) * 1.5f;
                trans.ValueRW.Position.z = (i / config.Size - center) * 1.5f;
                i++;
            }
        }
    }
}