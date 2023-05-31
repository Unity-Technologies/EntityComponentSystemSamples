using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Common.Scripts
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial struct RandomMotionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RandomMotion>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<PhysicsStep>(out var stepComponent))
                stepComponent = PhysicsStep.Default;

            state.Dependency = new EntityRandomMotionJob
            {
                Random = new Random(),
                DeltaTime = SystemAPI.Time.DeltaTime,
                StepComponent = stepComponent
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct EntityRandomMotionJob : IJobEntity
        {
            public Random Random;
            public PhysicsStep StepComponent;
            public float DeltaTime;

            public void Execute(ref RandomMotion motion, ref PhysicsVelocity velocity, in LocalTransform transform, in PhysicsMass mass)
            {
                motion.CurrentTime += DeltaTime;

                Random.InitState((uint)(motion.CurrentTime * 1000));

                var currentOffset = transform.Position - motion.InitialPosition;
                var desiredOffset = motion.DesiredPosition - motion.InitialPosition;
                // If we are close enough to the destination pick a new destination
                if (math.lengthsq(transform.Position - motion.DesiredPosition) < motion.Tolerance)
                {
                    var min = new float3(-math.abs(motion.Range));
                    var max = new float3(math.abs(motion.Range));
                    desiredOffset = Random.NextFloat3(min, max);
                    motion.DesiredPosition = desiredOffset + motion.InitialPosition;
                }
                var offset = desiredOffset - currentOffset;
                // Smoothly change the linear velocity
                velocity.Linear = math.lerp(velocity.Linear, offset, motion.Speed);
                if (mass.InverseMass != 0)
                {
                    velocity.Linear -= StepComponent.Gravity * DeltaTime;
                }
            }
        }
    }
}
