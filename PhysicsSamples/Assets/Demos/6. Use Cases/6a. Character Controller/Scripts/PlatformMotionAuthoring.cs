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
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new PlatformMotion
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
public partial struct PlatformMotionSystem : ISystem
{
    [BurstCompile]
    public partial struct MovePlatformsJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref PlatformMotion motion, ref PhysicsVelocity velocity, in LocalTransform localTransform)
        {
            motion.CurrentTime += DeltaTime;

            var desiredOffset = motion.Height * math.sin(motion.CurrentTime * motion.Speed);

            var currentOffset = math.dot(localTransform.Position - motion.InitialPosition, motion.Direction);

            velocity.Linear = motion.Direction * (desiredOffset - currentOffset);
            velocity.Angular = motion.Rotation;
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new MovePlatformsJob()
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
        }.Schedule(state.Dependency);
    }
}
