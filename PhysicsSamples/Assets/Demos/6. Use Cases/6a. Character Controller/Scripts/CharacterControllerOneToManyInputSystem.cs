using Unity.Burst;
using Unity.Entities;

// This input system simply applies the same character input
// information to every character controller in the scene
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(DemoInputGatheringSystem))]
public partial struct CharacterControllerOneToManyInputSystem : ISystem
{
    public partial struct CharacterControllerOneToManyInputSystemJobParallel : IJobEntity
    {
        public CharacterControllerInput Input;

        public void Execute(ref CharacterControllerInternalData ccData)
        {
            ccData.Input.Movement = Input.Movement;
            ccData.Input.Looking = Input.Looking;
            // jump request may not be processed on this frame, so record it rather than matching input state
            if (Input.Jumped != 0)
                ccData.Input.Jumped = 1;
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new CharacterControllerOneToManyInputSystemJobParallel
        {
            // Read user input
            Input = SystemAPI.GetSingleton<CharacterControllerInput>()
        }.ScheduleParallel(state.Dependency);
    }
}
