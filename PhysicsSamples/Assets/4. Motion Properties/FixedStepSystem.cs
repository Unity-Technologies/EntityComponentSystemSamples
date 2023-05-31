using Unity.Collections;
using Unity.Entities;

namespace MotionProperties
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct FixedStepSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FixedStep>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var sysGroup = state.World.GetOrCreateSystemManaged<FixedStepSimulationSystemGroup>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var(fixedStep, entity) in
                     SystemAPI.Query<RefRW<FixedStep>>()
                         .WithEntityAccess())
            {
                sysGroup.Timestep = fixedStep.ValueRW.Value;
                ecb.DestroyEntity(entity);
            }
            ecb.Playback(state.EntityManager);
        }
    }
}
