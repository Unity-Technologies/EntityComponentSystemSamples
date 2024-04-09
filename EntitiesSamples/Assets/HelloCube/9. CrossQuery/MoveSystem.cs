using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace HelloCube.CrossQuery
{
    public partial struct MoveSystem : ISystem
    {
        public float moveTimer;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ExecuteCrossQuery>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            moveTimer += dt;

            // periodically reverse direction and reset timer
            bool flip = false;
            if (moveTimer > 3.0f)
            {
                moveTimer = 0;
                flip = true;
            }

            foreach (var (transform, velocity) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRW<Velocity>>())
            {
                if (flip)
                {
                    velocity.ValueRW.Value *= -1;
                }

                // move
                transform.ValueRW.Position += velocity.ValueRO.Value * dt;
            }
        }
    }
}
