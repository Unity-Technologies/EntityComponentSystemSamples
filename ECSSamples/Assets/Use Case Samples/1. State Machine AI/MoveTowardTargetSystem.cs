using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Moves any guards that have a TargetPosition towards their target
/// A Target can be either a waypoint or a player (though here all we know is the position of the target)
/// </summary>
[RequireMatchingQueriesForUpdate]
public partial class MoveTowardTargetSystem : SystemBase
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
            .WithName("MoveTowardTarget") // ForEach name is helpful for debugging
            .ForEach((
#if !ENABLE_TRANSFORM_V1
                ref LocalToWorldTransform guardTransform, // "ref" keyword makes the parameter ReadWrite
#else
                ref Translation guardPosition, // "ref" keyword makes the parameter ReadWrite
                ref Rotation guardRotation,
#endif
                in TargetPosition targetPosition, // "in" keyword makes the parameter ReadOnly
                in MovementSpeed movementSpeed) =>
                {
                    // Determine if we are within the StopDistance of our target.
#if !ENABLE_TRANSFORM_V1
                    var vectorToTarget = targetPosition.Value - guardTransform.Value.Position;
#else
                    var vectorToTarget = targetPosition.Value - guardPosition.Value;
#endif
                    if (math.lengthsq(vectorToTarget) > GuardAIUtility.kStopDistanceSq)
                    {
                        // Normalize the vector to our target - this will be our movement direction
                        var moveDirection = math.normalize(vectorToTarget);

                        // Rotate the guard toward the target
                        // Since the camera looks at the scene top-down, we do not rotate in the y direction
#if !ENABLE_TRANSFORM_V1
                        guardTransform.Value.Rotation = quaternion.LookRotation(new float3(moveDirection.x, 0.0f, moveDirection.z), math.up());
#else
                        guardRotation.Value = quaternion.LookRotation(new float3(moveDirection.x, 0.0f, moveDirection.z), math.up());
#endif
                        // Move the guard forward toward the target
#if !ENABLE_TRANSFORM_V1
                        guardTransform.Value.Position += moveDirection * movementSpeed.MetersPerSecond * deltaTime;
#else
                        guardPosition.Value = guardPosition.Value + moveDirection * movementSpeed.MetersPerSecond * deltaTime;
#endif
                    }
                }).ScheduleParallel(); // Schedule the ForEach with the job system to run
    }
}
