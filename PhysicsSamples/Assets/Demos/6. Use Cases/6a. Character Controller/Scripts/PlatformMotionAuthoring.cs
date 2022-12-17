using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

public struct PlatformMotion : IComponentData
{
    public float CurrentTime;
    public float3 InitialPosition;
    public float Height;
    public float Speed;
    public float3 Direction;
    public float3 Rotation;
}

public class PlatformMotionAuthoring : MonoBehaviour
{
    public float Height = 1f;
    public float Speed = 1f;
    public float3 Direction = math.up();
    public float3 Rotation = float3.zero;
}

class PlatformMotionBaker : Baker<PlatformMotionAuthoring>
{
    public override void Bake(PlatformMotionAuthoring authoring)
    {
        AddComponent(new PlatformMotion
        {
            InitialPosition = authoring.transform.position,
            Height = authoring.Height,
            Speed = authoring.Speed,
            Direction = math.normalizesafe(authoring.Direction),
            Rotation = authoring.Rotation,
        });
    }
}

[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
[BurstCompile]
public partial struct PlatformMotionSystem : ISystem
{
    [BurstCompile]
    public partial struct MovePlatformsJob : IJobEntity
    {
        public float DeltaTime;

        [BurstCompile]
#if !ENABLE_TRANSFORM_V1
        public void Execute(ref PlatformMotion motion, ref PhysicsVelocity velocity, in LocalTransform localTransform)
#else
        public void Execute(ref PlatformMotion motion, ref PhysicsVelocity velocity, in Translation position)
#endif
        {
            motion.CurrentTime += DeltaTime;

            var desiredOffset = motion.Height * math.sin(motion.CurrentTime * motion.Speed);
#if !ENABLE_TRANSFORM_V1
            var currentOffset = math.dot(localTransform.Position - motion.InitialPosition, motion.Direction);
#else
            var currentOffset = math.dot(position.Value - motion.InitialPosition, motion.Direction);
#endif
            velocity.Linear = motion.Direction * (desiredOffset - currentOffset);
            velocity.Angular = motion.Rotation;
        }
    }

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
        // TODO(DOTS-6141): This expression can't currently be inlined into the IJobEntity initializer
        float dt = SystemAPI.Time.DeltaTime;
        state.Dependency = new MovePlatformsJob()
        {
            DeltaTime = dt,
        }.Schedule(state.Dependency);
    }
}
