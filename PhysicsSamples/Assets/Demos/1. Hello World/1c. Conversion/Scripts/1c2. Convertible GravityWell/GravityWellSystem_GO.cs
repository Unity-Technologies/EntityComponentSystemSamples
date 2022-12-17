using Unity.Burst;
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
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
[BurstCompile]
public partial struct GravityWellSystem_GO_ECS : ISystem
{
    private EntityQuery GravityWellQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<LocalToWorld>()
            .WithAllRW<GravityWellComponent_GO_ECS>();

        // Query equivalent to GameObject.FindObjectsOfType<GravityWellComponent_GO>
        GravityWellQuery = state.GetEntityQuery(builder);
        // Only need to update the GravityWellSystem if there are any entities with a GravityWellComponent
        state.RequireForUpdate(GravityWellQuery);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Update all the gravity well component positions from the entity's transform
        foreach (var(gravityWell, transform) in SystemAPI.Query<RefRW<GravityWellComponent_GO_ECS>, RefRO<LocalToWorld>>())
        {
            gravityWell.ValueRW.Position = transform.ValueRO.Position;
        }

        // Pull all the Gravity Well component data into a contiguous array
        using (var gravityWells = GravityWellQuery.ToComponentDataArray<GravityWellComponent_GO_ECS>(Allocator.TempJob))
        {
            // For each dynamic body apply the forces for all the gravity wells
            // Query equivalent to GameObject.FindObjectsOfType<Rigidbody>
#if !ENABLE_TRANSFORM_V1
            foreach (var(velocity, collider, mass, localTransform) in SystemAPI.Query<RefRW<PhysicsVelocity>, RefRO<PhysicsCollider>, RefRO<PhysicsMass>, RefRO<LocalTransform>>())
#else
            foreach (var(velocity, collider, mass, position, rotation) in SystemAPI.Query<RefRW<PhysicsVelocity>, RefRO<PhysicsCollider>, RefRO<PhysicsMass>, RefRO<Translation>, RefRO<Rotation>>())
#endif
            {
                for (int i = 0; i < gravityWells.Length; i++)
                {
                    var gravityWell = gravityWells[i];
                    velocity.ValueRW.ApplyExplosionForce(
#if !ENABLE_TRANSFORM_V1
                        mass.ValueRO, collider.ValueRO, localTransform.ValueRO.Position, localTransform.ValueRO.Rotation,
#else
                        mass.ValueRO, collider.ValueRO, position.ValueRO.Value, rotation.ValueRO.Value,
#endif
                        -gravityWell.Strength, gravityWell.Position, gravityWell.Radius,
                        SystemAPI.Time.DeltaTime, math.up());
                }
            }
        }
    }
}
#endregion
