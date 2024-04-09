#if !UNITY_DISABLE_MANAGED_COMPONENTS
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Animator = UnityEngine.Animator;
using Random = Unity.Mathematics.Random;

namespace Graphical.AnimationWithGameObjects
{
    public partial struct WanderSystem : ISystem
    {
        int isMovingID;

        public void OnCreate(ref SystemState state)
        {
            isMovingID = Animator.StringToHash("IsMoving");
            state.RequireForUpdate<ExecuteAnimationWithGameObjects>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var movement = SystemAPI.Time.DeltaTime * 3f;
            var time = (float)SystemAPI.Time.ElapsedTime;
            var random = Random.CreateFromIndex(state.GlobalSystemVersion);

            var animatorQuery = SystemAPI.QueryBuilder().WithAll<WanderState, Animator>().Build();
            var entities = animatorQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var wanderState = state.EntityManager.GetComponentData<WanderState>(entity);
                var animator = state.EntityManager.GetComponentObject<Animator>(entity);
                var timeLeft = wanderState.NextActionTime - time;
                bool isMoving = timeLeft > wanderState.Period / 2f;
                animator.SetBool(isMovingID, isMoving);
            }

            foreach (var (transform, wanderState) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRW<WanderState>>())
            {
                var timeLeft = wanderState.ValueRO.NextActionTime - time;

                if (timeLeft < 0f)
                {
                    wanderState.ValueRW.Period = random.NextFloat(2f, 5f);
                    wanderState.ValueRW.NextActionTime += wanderState.ValueRO.Period;
                    var angle = random.NextFloat(0f, math.PI * 2f);
                    transform.ValueRW.Rotation = quaternion.RotateY(angle);
                }
                else
                {
                    bool isMoving = timeLeft > wanderState.ValueRO.Period / 2f;

                    if (isMoving)
                    {
                        transform.ValueRW.Position += transform.ValueRO.Forward() * movement;
                    }
                }
            }
        }
    }
}
#endif
