using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Miscellaneous.CrossQuery
{
    public partial struct MoveSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponent<MoveTimer>(state.SystemHandle);
            state.RequireForUpdate<Execute.CrossQuery>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            var timer = state.EntityManager.GetComponentDataRW<MoveTimer>(state.SystemHandle);
            timer.ValueRW.Value += dt;

            // periodically reverse direction and reset timer
            bool flip = false;
            if (timer.ValueRO.Value > 3.0f)
            {
                timer.ValueRW.Value = 0;
                flip = true;
            }

            foreach (var (transform, velocity) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<Velocity>>())
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

    public struct MoveTimer : IComponentData
    {
        public float Value;
    }
}
