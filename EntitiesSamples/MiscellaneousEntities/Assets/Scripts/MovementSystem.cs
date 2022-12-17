using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace ClosestTarget
{
    [BurstCompile]
    public partial struct MovementSystem : ISystem
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
            var job = new MovementJob { Delta = SystemAPI.Time.DeltaTime * 10 };
            job.ScheduleParallelByRef();
        }
    }

    [BurstCompile]
    public partial struct MovementJob : IJobEntity
    {
        public float Delta;

        void Execute(ref LocalTransform transform, in Movement movement)
        {
            const float size = 100f;

            transform.Position.xz += movement.Value * Delta;
            if (transform.Position.x < -size) transform.Position.x += size * 2;
            if (transform.Position.x > +size) transform.Position.x -= size * 2;
            if (transform.Position.z < -size) transform.Position.z += size * 2;
            if (transform.Position.z > +size) transform.Position.z -= size * 2;
        }
    }
}