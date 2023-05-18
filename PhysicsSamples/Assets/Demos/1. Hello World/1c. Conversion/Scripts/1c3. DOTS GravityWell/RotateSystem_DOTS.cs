using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(GravityWellSystem_DOTS))]
public partial struct RotateSystem_DOTS : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var queryBuilder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<RotateComponent_DOTS>()
            .WithAllRW<LocalTransform>();

        // Only need to update the system if there are any entities with the associated component.
        state.RequireForUpdate(state.GetEntityQuery(queryBuilder));
    }

    [BurstCompile]
    public partial struct RotateJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref LocalTransform localTransform, in RotateComponent_DOTS rotator)
        {
            var av = rotator.LocalAngularVelocity * DeltaTime;

            localTransform.Rotation = math.mul(localTransform.Rotation, quaternion.Euler(av));
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new RotateJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
        }.Schedule(state.Dependency);
    }
}
