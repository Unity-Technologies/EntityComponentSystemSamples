using System;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Math = Unity.Physics.Math;

[RequireComponent(typeof(PhysicsBodyAuthoring))]
public class ApplyRocketThrustAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    [Min(0)] public float Magnitude = 1.0f;
    public Vector3 LocalDirection = -Vector3.forward;
    public Vector3 LocalOffset = Vector3.zero;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new ApplyRocketThrust
        {
            Magnitude = Magnitude,
            Direction = LocalDirection.normalized,
            Offset = LocalOffset,
        });
    }

    public void OnDrawGizmos()
    {
        if (LocalDirection.Equals(Vector3.zero)) return;

        var originalColor = Gizmos.color;
        var originalMatrix = Gizmos.matrix;

        Gizmos.color = Color.red;

        // Calculate the final Physics Body runtime coordinate system which bakes out skew from non-uniform scaling in parent
        var worldFromLocalRigidTransform = Math.DecomposeRigidBodyTransform(transform.localToWorldMatrix);
        var worldFromLocal = Matrix4x4.TRS(worldFromLocalRigidTransform.pos, worldFromLocalRigidTransform.rot, Vector3.one);

        Vector3 directionWorld = worldFromLocal.MultiplyVector(LocalDirection.normalized);
        Vector3 offsetWorld = worldFromLocal.MultiplyPoint(LocalOffset);

        // Calculate the final world Thrust coordinate system from the world Body transform and local offset and direction
        Math.CalculatePerpendicularNormalized(directionWorld, out _, out var directionPerpendicular);
        var worldFromThrust = Matrix4x4.TRS(offsetWorld, Quaternion.LookRotation(directionWorld, directionPerpendicular), Vector3.one);

        Gizmos.matrix = worldFromThrust;

        float Shift = Magnitude * 0.1f;
        Gizmos.DrawFrustum(new Vector3(0, 0, -Shift), UnityEngine.Random.Range(1.0f, 2.5f), Magnitude, Shift, 1.0f);

        Gizmos.matrix = originalMatrix;
        Gizmos.color = originalColor;
    }
}

public struct ApplyRocketThrust : IComponentData
{
    public float Magnitude;
    public float3 Direction;
    public float3 Offset;
}


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(BuildPhysicsWorld))]
public class ApplyRocketThrustSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;

        Entities
            .WithName("ApplyRocketThrust")
            .WithBurst()
            .ForEach((ref ApplyRocketThrust rocket, ref Translation t, ref Rotation r, ref PhysicsVelocity pv, ref PhysicsMass pm) =>
            {
                // Newton's 3rd law states that for every action there is an equal and opposite reaction.
                // As this is a rocket thrust the impulse applied with therefore use negative Direction.
                float3 impulse = -rocket.Direction * rocket.Magnitude;
                impulse = math.rotate(r.Value, impulse);
                impulse *= deltaTime;

                float3 offset = math.rotate(r.Value, rocket.Offset) + t.Value;

                pv.ApplyImpulse(pm, t, r, impulse, offset);
            }).Schedule();
    }
}
