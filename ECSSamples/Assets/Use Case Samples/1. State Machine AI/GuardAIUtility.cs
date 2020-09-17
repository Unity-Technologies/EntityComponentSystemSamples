using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// A set of utility functions used to make the intent of ECB operations more clear
/// </summary>
public class GuardAIUtility
{
    // The squared distance from our target at which we've determine we have reached it
    public const float kStopDistanceSq = 0.4f;

    /// <summary>
    /// Used to remove the components needed to transition out of the Idling State
    /// </summary>
    /// <param name="ecb">The ECB used to transition between states</param>
    /// <param name="e">The guard entity we are modifying</param>
    /// <param name="index">A unique index per-guard entity. Used by the ECB to deterministically sort operations.</param>
    public static void TransitionFromIdle(EntityCommandBuffer.ParallelWriter ecb, Entity e, int index)
    {
        ecb.RemoveComponent<IdleTimer>(index, e);
    }

    /// <summary>
    /// Used to remove the components needed to transition out of the Patrolling State
    /// </summary>
    /// <param name="ecb">The ECB used to transition between states</param>
    /// <param name="e">The guard entity we are modifying</param>
    /// <param name="index">A unique index per-guard entity. Used by the ECB to deterministically sort operations.</param>
    public static void TransitionFromPatrolling(EntityCommandBuffer.ParallelWriter ecb, Entity e, int index)
    {
        ecb.RemoveComponent<TargetPosition>(index, e);
    }

    /// <summary>
    /// Used to remove the components needed to transition out of the Chasing State
    /// </summary>
    /// <param name="ecb">The ECB used to transition between states</param>
    /// <param name="e">The guard entity we are modifying</param>
    /// <param name="index">A unique index per-guard entity. Used by the ECB to deterministically sort operations.</param>
    public static void TransitionFromChasing(EntityCommandBuffer.ParallelWriter ecb, Entity e, int index)
    {
        ecb.RemoveComponent<IsChasingTag>(index, e);
        ecb.RemoveComponent<TargetPosition>(index, e);
    }

    /// <summary>
    /// Used to add the components needed to be in the Idling State
    /// </summary>
    /// <param name="ecb">The ECB used to transition between states</param>
    /// <param name="e">The guard entity we are modifying</param>
    /// <param name="index"></param>
    public static void TransitionToIdle(EntityCommandBuffer.ParallelWriter ecb, Entity e, int index)
    {
        ecb.AddComponent(index, e, new IdleTimer {Value = 0.0f});
#if !UNITY_DISABLE_MANAGED_COMPONENTS
        ecb.AddComponent<IsInTransitionTag>(index, e);
#endif
    }

    /// <summary>
    /// Used to add the components needed to be in the Chasing State
    /// </summary>
    /// <param name="ecb">The ECB used to transition between states</param>
    /// <param name="e">The guard entity we are modifying</param>
    /// <param name="index">A unique index per-guard entity. Used by the ECB to deterministically sort operations.</param>
    /// <param name="playerPosition">The position of the player we are setting as our new TargetPosition</param>
    public static void TransitionToChasing(EntityCommandBuffer.ParallelWriter ecb, Entity e, int index, float3 playerPosition)
    {
        ecb.AddComponent<IsChasingTag>(index, e);
        ecb.AddComponent(index, e, new TargetPosition {Value = playerPosition});
#if !UNITY_DISABLE_MANAGED_COMPONENTS
        ecb.AddComponent<IsInTransitionTag>(index, e);
#endif
    }

    /// <summary>
    /// Used to add the components needed to be in the Patrolling State
    /// </summary>
    /// <param name="ecb">The ECB used to transition between states</param>
    /// <param name="e">The guard entity we are modifying</param>
    /// <param name="index">A unique index per-guard entity. Used by the ECB to deterministically sort operations.</param>
    /// <param name="waypointPosition">The position of the waypoint we are setting as our new TargetPosition.</param>
    public static void TransitionToPatrolling(EntityCommandBuffer.ParallelWriter ecb, Entity e, int index, float3 waypointPosition)
    {
        ecb.AddComponent(index, e, new TargetPosition {Value = waypointPosition});
#if !UNITY_DISABLE_MANAGED_COMPONENTS
        ecb.AddComponent<IsInTransitionTag>(index, e);
#endif
    }
}
