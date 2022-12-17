using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public struct RandomMotion : IComponentData
{
    public float CurrentTime;
    public float3 InitialPosition;
    public float3 DesiredPosition;
    public float Speed;
    public float Tolerance;
    public float3 Range;
}

// This behavior will set a dynamic body's linear velocity to get to randomly selected
// point in space. When the body gets with a specified tolerance of the random position,
// a new random position is chosen and the body starts header there instead.
public class RandomMotionAuthoring : MonoBehaviour
{
    public float3 Range = new float3(1);
}

class RandomMotionAuthoringBaker : Baker<RandomMotionAuthoring>
{
    public override void Bake(RandomMotionAuthoring authoring)
    {
        var length = math.length(authoring.Range);
        var transform = GetComponent<Transform>();
        AddComponent(new RandomMotion
        {
            InitialPosition = transform.position,
            DesiredPosition = transform.position,
            Speed = length * 0.001f,
            Tolerance = length * 0.1f,
            Range = authoring.Range,
        });
    }
}

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial struct RandomMotionSystem : ISystem
{
    [BurstCompile]
    public partial struct JobEntityRandomMotion : IJobEntity
    {
        public Random Random;
        public PhysicsStep StepComponent;
        public float DeltaTime;

        [BurstCompile]
#if !ENABLE_TRANSFORM_V1
        public void Execute(ref RandomMotion motion, ref PhysicsVelocity velocity, in LocalTransform transform, in PhysicsMass mass)
#else
        public void Execute(ref RandomMotion motion, ref PhysicsVelocity velocity, in Translation position, in PhysicsMass mass)
#endif
        {
            motion.CurrentTime += DeltaTime;

            Random.InitState((uint)(motion.CurrentTime * 1000));
#if !ENABLE_TRANSFORM_V1
            var currentOffset = transform.Position - motion.InitialPosition;
            var desiredOffset = motion.DesiredPosition - motion.InitialPosition;
            // If we are close enough to the destination pick a new destination
            if (math.lengthsq(transform.Position - motion.DesiredPosition) < motion.Tolerance)
#else
            var currentOffset = position.Value - motion.InitialPosition;
            var desiredOffset = motion.DesiredPosition - motion.InitialPosition;
            // If we are close enough to the destination pick a new destination
            if (math.lengthsq(position.Value - motion.DesiredPosition) < motion.Tolerance)
#endif
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

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<RandomMotion>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var random = new Random();
        float dt = SystemAPI.Time.DeltaTime;
        if (!SystemAPI.TryGetSingleton<PhysicsStep>(out var stepComponent))
            stepComponent = PhysicsStep.Default;

        state.Dependency = new JobEntityRandomMotion
        {
            Random = random,
            DeltaTime = dt,
            StepComponent = stepComponent
        }.Schedule(state.Dependency);
    }
}
