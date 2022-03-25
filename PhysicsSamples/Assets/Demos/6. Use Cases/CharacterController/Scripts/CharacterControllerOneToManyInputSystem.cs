using Unity.Entities;

// This input system simply applies the same character input
// information to every character controller in the scene
[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(DemoInputGatheringSystem))]
public partial class CharacterControllerOneToManyInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Read user input
        var input = GetSingleton<CharacterControllerInput>();
        Entities
            .WithName("CharacterControllerOneToManyInputSystemJob")
            .WithBurst()
            .ForEach((ref CharacterControllerInternalData ccData) =>
            {
                ccData.Input.Movement = input.Movement;
                ccData.Input.Looking = input.Looking;
                // jump request may not be processed on this frame, so record it rather than matching input state
                if (input.Jumped != 0)
                    ccData.Input.Jumped = 1;
            }
            ).ScheduleParallel();
    }
}
