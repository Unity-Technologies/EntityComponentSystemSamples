using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Miscellaneous.StateChangeStructural
{
    public partial struct SpinSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Execute.StateChangeStructural>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new SpinJob
            {
                Offset = quaternion.RotateY(SystemAPI.Time.DeltaTime * math.PI)
            }.ScheduleParallel();
            state.Dependency.Complete();
        }
    }


    [WithAll(typeof(Spinner))]
    [BurstCompile]
    partial struct SpinJob : IJobEntity
    {
        public quaternion Offset;

        void Execute(ref LocalTransform transform)
        {
            transform = transform.Rotate(Offset);
        }
    }
}
