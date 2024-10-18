using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
public partial struct CubeMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var builder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Simulate>()
            .WithAll<CubeInput>()

            .WithAllRW<LocalTransform>();

        var query = state.GetEntityQuery(builder);
        state.RequireForUpdate(query);
    }
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var moveJob = new MoveCubeJob
        {
            tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick,
            fixedCubeSpeed = SystemAPI.Time.DeltaTime * 4
        };
        state.Dependency = moveJob.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    [WithAll(typeof(Simulate))]
    partial struct MoveCubeJob : IJobEntity
    {
        public NetworkTick tick;
        public float fixedCubeSpeed;

        public void Execute(CubeInput playerInput, ref LocalTransform trans)
        {
            var moveInput = new float2(playerInput.Horizontal, playerInput.Vertical);
            moveInput = math.normalizesafe(moveInput) * fixedCubeSpeed;
            trans.Position += new float3(moveInput.x, 0, moveInput.y);
        }
    }
}
