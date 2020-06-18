using Unity.Entities;
using Unity.Physics.Systems;

// This input system simply applies the same character input 
// information to every character controller in the scene
[UpdateAfter(typeof(ExportPhysicsWorld)), UpdateBefore(typeof(CharacterControllerSystem))]
public class CharacterControllerOneToManyInputSystem : SystemBase
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
                ccData.Input.Jumped = input.Jumped;
            }
        ).ScheduleParallel();
    }
}
