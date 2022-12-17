using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace CrossQuery
{
    public struct MoveTimer : IComponentData
    {
        public float Value;
    }
    
    [BurstCompile]
    public partial struct MoveSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponent<MoveTimer>(state.SystemHandle);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
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

            foreach (var (transform, velocity) in SystemAPI.Query<TransformAspect, RefRW<Velocity>>())
            {
                if (flip)
                {
                    velocity.ValueRW.Value *= -1;
                }
             
                // move
                transform.LocalPosition += velocity.ValueRO.Value * dt;
            }
        }
    }
}