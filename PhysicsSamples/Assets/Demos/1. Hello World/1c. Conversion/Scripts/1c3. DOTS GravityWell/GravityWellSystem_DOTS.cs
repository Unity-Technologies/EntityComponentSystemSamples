using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Burst;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial struct GravityWellSystem_DOTS : ISystem
{
    private EntityQuery GravityWellQuery;

    [BurstCompile]
    public partial struct GravityWellSystem_DOTS_ForEachGravityWell : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<GravityWellComponent_DOTS> GravityWells;

        public void Execute([EntityIndexInQuery] int entityIndexInQuery, ref GravityWellComponent_DOTS gravityWell, in LocalToWorld transform)
        {
            gravityWell.Position = transform.Position;
            GravityWells[entityIndexInQuery] = gravityWell;
        }
    }

    [BurstCompile]
    public partial struct GravityWellSystem_DOTS_ForEachDynamicBodies : IJobEntity
    {
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        [DeallocateOnJobCompletion]
        public NativeArray<GravityWellComponent_DOTS> GravityWells;
        public float DeltaTime;

        public void Execute(ref PhysicsVelocity velocity, in PhysicsCollider collider, in PhysicsMass mass, in LocalTransform localTransform)
        {
            for (int i = 0; i < GravityWells.Length; i++)
            {
                var gravityWell = GravityWells[i];
                velocity.ApplyExplosionForce(

                    mass, collider, localTransform.Position, localTransform.Rotation,

                    -gravityWell.Strength, gravityWell.Position, gravityWell.Radius,
                    DeltaTime, math.up());
            }
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<LocalToWorld>()
            .WithAllRW<GravityWellComponent_DOTS>();

        GravityWellQuery = state.GetEntityQuery(builder);
        // Only need to update the GravityWellSystem if there are any entities with a GravityWellComponent
        state.RequireForUpdate(GravityWellQuery);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var gravityWells = new NativeArray<GravityWellComponent_DOTS>(
            GravityWellQuery.CalculateEntityCount(), Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        // For each gravity well component update the position and add them to the array
        state.Dependency = new GravityWellSystem_DOTS_ForEachGravityWell
        {
            GravityWells = gravityWells
        }.ScheduleParallel(state.Dependency);

        state.Dependency = new GravityWellSystem_DOTS_ForEachDynamicBodies
        {
            GravityWells = gravityWells,
            DeltaTime = SystemAPI.Time.DeltaTime
        }.ScheduleParallel(state.Dependency);
    }
}
