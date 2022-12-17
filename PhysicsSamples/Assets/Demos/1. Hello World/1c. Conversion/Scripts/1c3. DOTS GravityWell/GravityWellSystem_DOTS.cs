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
[BurstCompile]
public partial struct GravityWellSystem_DOTS : ISystem
{
    private EntityQuery GravityWellQuery;

    [BurstCompile]
    public partial struct GravityWellSystem_DOTS_ForEachGravityWell : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<GravityWellComponent_DOTS> GravityWells;

        [BurstCompile]
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

        [BurstCompile]
#if !ENABLE_TRANSFORM_V1
        public void Execute(ref PhysicsVelocity velocity, in PhysicsCollider collider, in PhysicsMass mass, in LocalTransform localTransform)
#else
        public void Execute(ref PhysicsVelocity velocity, in PhysicsCollider collider, in PhysicsMass mass, in Translation position, in Rotation rotation)
#endif
        {
            for (int i = 0; i < GravityWells.Length; i++)
            {
                var gravityWell = GravityWells[i];
                velocity.ApplyExplosionForce(
#if !ENABLE_TRANSFORM_V1
                    mass, collider, localTransform.Position, localTransform.Rotation,
#else
                    mass, collider, position.Value, rotation.Value,
#endif
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
    public void OnDestroy(ref SystemState state)
    {
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

        // TODO(DOTS-6141): This expression can't currently be inlined into the IJobEntity initializer
        float dt = SystemAPI.Time.DeltaTime;
        state.Dependency = new GravityWellSystem_DOTS_ForEachDynamicBodies
        {
            GravityWells = gravityWells,
            DeltaTime = dt,
        }.ScheduleParallel(state.Dependency);
    }
}
