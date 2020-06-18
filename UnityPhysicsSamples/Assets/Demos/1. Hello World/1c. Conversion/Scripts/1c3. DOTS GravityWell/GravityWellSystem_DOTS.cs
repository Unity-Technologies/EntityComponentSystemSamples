using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Collections;

[UpdateBefore(typeof(BuildPhysicsWorld))]
public class GravityWellSystem_DOTS : SystemBase
{
    private EntityQuery GravityWellQuery;
    private BuildPhysicsWorld BuildPhysicsWorldSystem;

    protected override void OnCreate()
    {
        GravityWellQuery = GetEntityQuery(
                                ComponentType.ReadOnly<LocalToWorld>(),
                                typeof(GravityWellComponent_DOTS));
        // Only need to update the GravityWellSystem if there are any entities with a GravityWellComponent
        RequireForUpdate(GravityWellQuery);

        BuildPhysicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();
    }

    protected override void OnUpdate()
    {
        // Now that we are Scheduling the Entities.ForEach rather than Running them,
        // we need to grab the gravityWells for use in the next job. 
        // This would be unnecessary if the GravityWellQuery.ToComponentDataArray function 
        // can return a JobHandle that can be chained between the ForEach jobs.
        var gravityWells = new NativeArray<GravityWellComponent_DOTS>(
            GravityWellQuery.CalculateEntityCount(), Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        // For each gravity well component update the position and add them to the array
        Entities
        .WithName("GravityWellSystem_DOTS_ForEachGravityWell")
        .WithBurst()
        .WithNativeDisableParallelForRestriction(gravityWells)
        .WithChangeFilter<LocalToWorld>()
        .ForEach((Entity entity, int entityInQueryIndex, ref GravityWellComponent_DOTS gravityWell, in LocalToWorld transform) =>
        {
            gravityWell.Position = transform.Position;
            gravityWells[entityInQueryIndex] = gravityWell;
        }).Schedule();

        // Create local 'up' and 'deltaTime' variables so they are accessible inside the ForEach lambda
        var up = math.up();
        var deltaTime = Time.DeltaTime;

        // For each dynamic body apply the forces for all the gravity wells
        Entities
        .WithName("GravityWellSystem_DOTS_ForEachDynamicBodies")
        .WithBurst()
        .WithDeallocateOnJobCompletion(gravityWells)
        .ForEach((
            ref PhysicsVelocity velocity,
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
        }).ScheduleParallel();

        // Chain the scheduled jobs as dependencies into the BuildPhysicsWorld system
        BuildPhysicsWorldSystem.AddInputDependency(Dependency);
    }
}
