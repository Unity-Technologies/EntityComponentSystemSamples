using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

// Movement speed is used by both the Guard and the Player
struct MovementSpeed : IComponentData
{
    public float MetersPerSecond;
}

// System for moving the player based on keyboard input
[RequireMatchingQueriesForUpdate]
[UpdateAfter(typeof(GatherInputSystem))]
public partial class MovePlayerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Grab our DeltaTime out of the system so it is usable by the ForEach lambda
        var deltaTime = SystemAPI.Time.DeltaTime;

        // For simple systems that only run a single job and don't need to access the JobHandle themselves,
        // it can be omitted from the Entities.ForEach() call. The job will implicitly use the system's
        // Dependency handle as its input dependency, and update the system's Dependency property to contain the scheduled
        // job's handle.
        Entities
            .WithName("MovePlayer")// ForEach name is helpful for debugging
            .ForEach((
#if !ENABLE_TRANSFORM_V1
                ref LocalToWorldTransform transform, // "ref" keyword makes this parameter ReadWrite
#else
                ref Translation translation, // "ref" keyword makes this parameter ReadWrite
#endif
                in UserInputData input, // "in" keyword makes this parameter ReadOnly
                in MovementSpeed speed) =>
                {
#if !ENABLE_TRANSFORM_V1
                    transform.Value.Position += new float3(input.Move.x, 0.0f, input.Move.y) * speed.MetersPerSecond * deltaTime;
#else
                    translation.Value += new float3(input.Move.x, 0.0f, input.Move.y) * speed.MetersPerSecond * deltaTime;
#endif
                }).ScheduleParallel(); // Schedule the ForEach with the job system to run
    }
}
