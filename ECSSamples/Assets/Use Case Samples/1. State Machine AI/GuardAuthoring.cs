using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// An IBufferElementData becomes a DynamicBuffer<T>
// This will reserve 8 WaypointPositions of memory per-entity in the chunk
[InternalBufferCapacity(8)]
public struct WaypointPosition : IBufferElementData
{
    public float3 Value;
}

// The total time (in seconds) to wait during each idle state
public struct CooldownTime : IComponentData
{
    public float Value;
}

// The index of the next waypoint to travel to in our DynamicBuffer<WaypointPosition>
public struct NextWaypointIndex : IComponentData
{
    public int Value;
}

// A tag that helps us tell the difference between "Patrolling" and "Chasing"
public struct IsChasingTag : IComponentData
{}

// The position the guard plans to move towards during "Patrolling" and "Chasing"
public struct TargetPosition : IComponentData
{
    public float3 Value;
}

// Used to store how long (in seconds) we have been in the idle state. Only exists when "Idle"
public struct IdleTimer : IComponentData
{
    public float Value;
}

// Defines a 2D cone in front of the guard, used to detect players
public struct VisionCone : IComponentData
{
    public float AngleRadians;
    public float ViewDistanceSq;
}

// A tag that helps us tell the difference between "Patrolling" and "Chasing"
public struct IsInTransitionTag : IComponentData
{}

/// <summary>
/// "Authoring" component for guards. Part of the GameObject Conversion workflow.
/// Allows us to edit GameObjects in the Editor and convert those GameObjects to the optimized Entity representation
/// </summary>
[DisallowMultipleComponent]
public class GuardAuthoring : MonoBehaviour
{
    /// <summary>
    /// We can refer directly to GameObject transforms in the authoring component
    /// </summary>
    public List<Transform> Waypoints;

    // Fields are used to populate Entity data
    public float IdleCooldownTime = 3.0f;
    public float VisionAngleDegrees = 45.0f;
    public float VisionMaxDistance = 5.0f;
    public float MovementSpeedMetersPerSecond = 3.0f;

    class Baker : Baker<GuardAuthoring>
    {
        public override void Bake(GuardAuthoring authoring)
        {
            // Here we add all of the components needed to start the guard off in the "Patrol" state
            // i.e. We add TargetPosition, and don't add IdleTimer or IsChasing tag
            AddComponent(GetEntity(), new ComponentTypeSet(
                new ComponentType[]
                {
                    typeof(CooldownTime),
                    typeof(NextWaypointIndex),
                    typeof(TargetPosition),
                    typeof(WaypointPosition),
                    typeof(VisionCone),
                    typeof(MovementSpeed),
                    typeof(IsInTransitionTag)
                }));

            // Since we've already added the WaypointPosition IBufferElementData to the entity, we need to Get the buffer to fill it
            var buffer = SetBuffer<WaypointPosition>();
            foreach (var waypointTransform in authoring.Waypoints)
            {
                buffer.Add(new WaypointPosition { Value = waypointTransform.position });
            }

            var entity = GetEntity();

            // Transfer the values from the authoring component to the entity data
            SetComponent( entity, new CooldownTime {Value = authoring.IdleCooldownTime});
            SetComponent( entity, new NextWaypointIndex {Value = 0});
            SetComponent( entity, new TargetPosition {Value = buffer[0].Value});
            SetComponent( entity, new MovementSpeed {MetersPerSecond = authoring.MovementSpeedMetersPerSecond});

            // Note: The authoring component uses Degrees and non-squared distance, while the runtime uses radians and squared distance
            // We want the authoring component to use the most user-friendly data formats, but the runtime to use a more optimized format
            // GameObject Conversion is perfect for this. During conversion, we convert to the more optimized units.
            SetComponent(entity, new VisionCone {AngleRadians = math.radians(authoring.VisionAngleDegrees), ViewDistanceSq = authoring.VisionMaxDistance * authoring.VisionMaxDistance});
        }
    }
}
