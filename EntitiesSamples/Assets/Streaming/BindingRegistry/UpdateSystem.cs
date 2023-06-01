using Unity.Burst;
using Unity.Entities;

namespace Streaming.BindingRegistry
{
    public partial struct UpdateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Example>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var binding in
                     SystemAPI.Query<RefRW<Example>>())
            {
                binding.ValueRW.Float += 1.0f;
                binding.ValueRW.Int += 1;
                binding.ValueRW.Bool = !binding.ValueRO.Bool;
            }
        }
    }
}
