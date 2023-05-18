using Unity.Burst;
using Unity.Entities;

namespace Miscellaneous.StateChange
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [BurstCompile]
    public partial struct ProfilerModuleSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponent<StateChangeProfilerModule.FrameData>(entity);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            ref var frameData = ref SystemAPI.GetSingletonRW<StateChangeProfilerModule.FrameData>().ValueRW;
            StateChangeProfilerModule.SpinPerf = frameData.RotatePerf;
            StateChangeProfilerModule.UpdatePerf = frameData.SetStatePerf;
        }
    }
}