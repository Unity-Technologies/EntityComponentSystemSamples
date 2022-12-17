using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace StateMachineValue
{
    [BurstCompile]
    public partial struct SpinSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new SpinJob
            {
                Offset = quaternion.RotateY(SystemAPI.Time.DeltaTime * math.PI)
            };
            job.ScheduleParallel();
        }
    }

    [BurstCompile]
    partial struct SpinJob : IJobEntity
    {
        public quaternion Offset;

        void Execute(TransformAspect transform, in Cube cube)
        {
            if (cube.IsSpinning)
            {
                transform.RotateLocal(Offset);
            }
        }
    }
}