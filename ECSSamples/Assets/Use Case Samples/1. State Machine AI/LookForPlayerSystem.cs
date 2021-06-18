using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Checks whether the player is within guard vision cones
/// If the player is seen, chase the player
/// </summary>
public partial class LookForPlayerSystem : SystemBase
{
    // Cache a reference to this system in OnCreate() to prevent World.GetExistingSystem being called every frame
    private EndSimulationEntityCommandBufferSystem m_EndSimECBSystem;

    // Used to quickly query for player position
    private EntityQuery m_PlayerQuery;

    protected override void OnCreate()
    {
        m_EndSimECBSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        m_PlayerQuery = GetEntityQuery(ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<Translation>());
    }

    protected override void OnUpdate()
    {
        // This determines the number of players we have and allocates a NativeArray of Translations
        // It then schedules a job to be run which will fill that array
        // We pass the output job handle of this function as a dependency of the ForEach below to guarantee it completes before we need the player position
        var playerPosition = m_PlayerQuery.ToComponentDataArrayAsync<Translation>(Allocator.TempJob, out JobHandle getPositionHandle);


        // ECB which will run at the end of the simulation group. If the player is seen, will change the Guard's state to chasing
        var ecb = m_EndSimECBSystem.CreateCommandBuffer().AsParallelWriter();

        // Update the target position (must pass isReadOnly=false)
        var targetPositionFromEntity = GetComponentDataFromEntity<TargetPosition>(false);

        var lookHandle = Entities
            .WithName("LookForPlayer") // ForEach name is helpful for debugging
            .WithReadOnly(playerPosition) // Marked read only (We don't mutate the player position)
            .WithNativeDisableParallelForRestriction(targetPositionFromEntity) // Since ComponentDataFromEntity allows us to access *any* entity,
                                                                               // we need to disable the safety system (since we know we're writing
                                                                               // to the player and not another guard)
            .WithDisposeOnCompletion(playerPosition) // Prevent a memory leak by deallocating our position array at the end of the job
            .ForEach((
                Entity guardEntity, // Refers to the current guard entity. Used by the ECB when changing states
                int entityInQueryIndex, // Index of the guard entity in the query. Used for Concurrent ECB writing
                in Translation guardPosition, // "in" keyword makes the parameter ReadOnly
                in Rotation guardRotation,
                in VisionCone guardVisionCone
                ) =>
                {
                    // If there are no players, we can safely skip this work
                    if (playerPosition.Length <= 0)
                    {
                        return;
                    }

                    // Get a normalized vector from the guard to the player
                    var forwardVector = math.forward(guardRotation.Value);
                    var vectorToPlayer = playerPosition[0].Value - guardPosition.Value;
                    var unitVecToPlayer = math.normalize(vectorToPlayer);

                    // Use the dot product to determine if the player is within our vision cone
                    var dot = math.dot(forwardVector, unitVecToPlayer);
                    var canSeePlayer = dot > 0.0f && // player is in front of us
                        math.abs(math.acos(dot)) < guardVisionCone.AngleRadians &&            // player is within the cone angle bounds
                        math.lengthsq(vectorToPlayer) < guardVisionCone.ViewDistanceSq;            // player is within vision distance (we use Squared Distance to avoid sqrt calculation)

                    // Here we grab the tag of the guardEntity
                    var isCurrentlyChasing = HasComponent<IsChasingTag>(guardEntity);

                    if (canSeePlayer)
                    {
                        if (isCurrentlyChasing)
                        {
                            // Update the target position of the guard to the player the guard is chasing
                            // Here we use ComponentDataFromEntity because idle guards will not have a TargetPosition
                            // If the guard is already chasing, then we know they have a TargetPosition already and can set its value
                            targetPositionFromEntity[guardEntity] = new TargetPosition {Value = playerPosition[0].Value};
                        }
                        else
                        {
                            GuardAIUtility.TransitionToChasing(ecb, guardEntity, entityInQueryIndex, playerPosition[0].Value);
                        }

                        // If the guard has an idle timer, we want to leave the idle state
                        // Therefore, we remove the timer from the guard
                        if (HasComponent<IdleTimer>(guardEntity))
                        {
                            GuardAIUtility.TransitionFromIdle(ecb, guardEntity, entityInQueryIndex);
                        }
                    }
                    else if (isCurrentlyChasing) // If we don't see the player anymore, stop chasing
                    {
                        GuardAIUtility.TransitionFromChasing(ecb, guardEntity, entityInQueryIndex);
                        GuardAIUtility.TransitionToIdle(ecb, guardEntity, entityInQueryIndex);
                    }
                }).ScheduleParallel(getPositionHandle); // Schedule the ForEach with the job system to run
        // Note here we pass the getPositionHandle as a dependency
        // This will ensure that the previous ToComponentDataArray
        // operation will finish before our ForEach runs

        // EntityCommandBufferSystems need to know about all jobs which write to EntityCommandBuffers it has created
        m_EndSimECBSystem.AddJobHandleForProducer(lookHandle);

        // Pass the handle generated by the ForEach to the next system
        Dependency = lookHandle;
    }
}
