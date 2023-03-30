using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Samples.HelloNetcode
{
    [UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [BurstCompile]
    public partial struct SpinnerSystem : ISystem
    {
        [BurstCompile]
        partial struct SpinnerJob : IJobEntity
        {
            public float DeltaTime;

            [BurstCompile]
            public void Execute(ref LocalTransform transform)
            {
                transform = transform.RotateX(DeltaTime);
            }
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableOptimization>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new SpinnerJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.ScheduleParallel(state.Dependency);
        }
    }
}
