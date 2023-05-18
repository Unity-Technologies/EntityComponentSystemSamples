using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Miscellaneous.StateChangeValue
{
    public partial struct SpinSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Execute.StateChangeValue>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new SpinJob
            {
                Offset = quaternion.RotateY(SystemAPI.Time.DeltaTime * math.PI)
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    partial struct SpinJob : IJobEntity
    {
        public quaternion Offset;

        void Execute(ref LocalTransform transform, in Cube cube)
        {
            if (cube.IsSpinning)
            {
                transform = transform.Rotate(Offset);
            }
        }
    }
}
