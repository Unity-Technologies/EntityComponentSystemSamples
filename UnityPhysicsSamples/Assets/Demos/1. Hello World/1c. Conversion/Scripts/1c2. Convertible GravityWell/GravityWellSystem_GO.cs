using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;

public class GravityWellSystem_GO : MonoBehaviour
{
    void Update()
    {
        Profiler.BeginSample("GravityWellSystem_GO:Update");

        // Apply force from all Gravity Wells to all Rigidbody components
        var gravityWells = GameObject.FindObjectsOfType<GravityWellComponent_GO>();
        foreach (var dynamicBody in GameObject.FindObjectsOfType<Rigidbody>())
        {
            foreach (var gravityWell in gravityWells)
            {
                var gravityWellPosition = gravityWell.gameObject.transform.position;
                dynamicBody.AddExplosionForce(-gravityWell.Strength, gravityWellPosition, gravityWell.Radius);
            }
        }

        Profiler.EndSample();
    }
}

#region ECS
[UpdateBefore(typeof(BuildPhysicsWorld))]
public class GravityWellSystem_GO_ECS : SystemBase
{
    private EntityQuery GravityWellQuery;

    protected override void OnCreate()
    {
        // Query equivalent to GameObject.FindObjectsOfType<GravityWellComponent_GO>
        GravityWellQuery = GetEntityQuery(
                                ComponentType.ReadOnly<LocalToWorld>(), 
                                typeof(GravityWellComponent_GO_ECS));
        // Only need to update the GravityWellSystem if there are any entities with a GravityWellComponent
        RequireForUpdate(GravityWellQuery);
    }

    protected override void OnUpdate()
    {
        Profiler.BeginSample("GravityWellSystem_GO_ECS:Update");

        // Update all the gravity well component positions from the entity's transform
        Entities
        .WithBurst()
        .ForEach((ref GravityWellComponent_GO_ECS gravityWell, in LocalToWorld transform) =>
        {
            gravityWell.Position = transform.Position;
        }).Run();

        // Create local 'up' and 'deltaTime' variables so they are accessible inside the ForEach lambda
        var up = math.up();
        var deltaTime = Time.DeltaTime;

        // Pull all the Gravity Well component data into a contiguous array
        using (var gravityWells = GravityWellQuery.ToComponentDataArray<GravityWellComponent_GO_ECS>(Allocator.TempJob))
        {
            // For each dynamic body apply the forces for all the gravity wells
            // Query equivalent to GameObject.FindObjectsOfType<Rigidbody>
            Entities
            .WithBurst()
            .WithReadOnly(gravityWells)
            .ForEach((ref PhysicsVelocity velocity,
                in PhysicsCollider collider, in PhysicsMass mass,
                in Translation position, in Rotation rotation) =>
            {
                for (int i = 0; i < gravityWells.Length; i++)
                {
                    var gravityWell = gravityWells[i];
                    velocity.ApplyExplosionForce(
                            mass, collider, position, rotation,
                            -gravityWell.Strength, gravityWell.Position, gravityWell.Radius,
                            deltaTime, up);
                }
            }).Run();
        }

        Profiler.EndSample();
    }
}
#endregion
