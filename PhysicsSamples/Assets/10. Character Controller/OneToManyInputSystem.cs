using Unity.Burst;
using Unity.Entities;

namespace CharacterController
{
// This input system simply applies the same character input
// information to every character controller in the scene
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(Common.Scripts.DemoInputGatheringSystem))]
    public partial struct CharacterControllerOneToManyInputSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Input>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new InputJob
            {
                // Read user input
                Input = SystemAPI.GetSingleton<Input>()
            }.ScheduleParallel(state.Dependency);
        }
    }

    public partial struct InputJob : IJobEntity
    {
        public Input Input;

        public void Execute(ref CharacterControllerInternal cc)
        {
            cc.Input.Movement = Input.Movement;
            cc.Input.Looking = Input.Looking;
            // jump request may not be processed on this frame, so record it rather than matching input state
            if (Input.Jumped != 0)
            {
                cc.Input.Jumped = 1;
            }
        }
    }
}
