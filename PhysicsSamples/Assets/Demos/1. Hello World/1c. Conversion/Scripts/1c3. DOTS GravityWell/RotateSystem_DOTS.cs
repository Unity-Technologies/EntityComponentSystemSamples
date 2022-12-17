using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(GravityWellSystem_DOTS))]
[BurstCompile]
public partial struct RotateSystem_DOTS : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var queryBuilder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<RotateComponent_DOTS>()
#if !ENABLE_TRANSFORM_V1
            .WithAllRW<LocalTransform>();
#else
            .WithAllRW<Rotation>();
#endif

        // Only need to update the system if there are any entities with the associated component.
        state.RequireForUpdate(state.GetEntityQuery(queryBuilder));
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public partial struct Rotate_Job : IJobEntity
    {
        public float DeltaTime;

        [BurstCompile]
#if !ENABLE_TRANSFORM_V1
        public void Execute(ref LocalTransform localTransform, in RotateComponent_DOTS rotator)
#else
        public void Execute(ref Rotation rotation, in RotateComponent_DOTS rotator)
#endif
        {
            var av = rotator.LocalAngularVelocity * DeltaTime;
#if !ENABLE_TRANSFORM_V1
            localTransform.Rotation = math.mul(localTransform.Rotation, quaternion.Euler(av));
#else
            rotation.Value = math.mul(rotation.Value, quaternion.Euler(av));
#endif
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // TODO(DOTS-6141): This expression can't currently be inlined into the IJobEntity initializer
        float dt = SystemAPI.Time.DeltaTime;
        state.Dependency = new Rotate_Job
        {
            DeltaTime = dt,
        }.Schedule(state.Dependency);
    }
}
