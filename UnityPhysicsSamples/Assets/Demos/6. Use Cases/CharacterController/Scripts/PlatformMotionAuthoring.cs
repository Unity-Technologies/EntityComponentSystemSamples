using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public struct PlatformMotion : IComponentData
{
    public float CurrentTime;
    public float3 InitialPosition;
    public float3 DesiredPosition;
    public float Height;
    public float Speed;
    public float3 Direction;
    public float3 Rotation;
}

public class PlatformMotionAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float Height = 1f;
    public float Speed = 1f;
    public float3 Direction = math.up();
    public float3 Rotation = float3.zero;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData<PlatformMotion>(entity, new PlatformMotion
        {
            InitialPosition = transform.position,
            DesiredPosition = transform.position,
            Height = Height,
            Speed = Speed,
            Direction = math.normalizesafe(Direction),
            Rotation = Rotation,
        });
    }
}


[UpdateBefore(typeof(BuildPhysicsWorld))]
public class PlatformMotionSystem : JobComponentSystem
{
    protected override void OnCreate()
    {
    }

    protected struct PlatformMotionJob : IJobForEach<PlatformMotion, Translation, PhysicsVelocity>
    {
        public Random random;
        public float deltaTime;

        public void Execute(ref PlatformMotion motion, [ReadOnly] ref Translation position, ref PhysicsVelocity velocity)
        {
            motion.CurrentTime += deltaTime;

            var desiredOffset = motion.Height * math.sin(motion.CurrentTime * motion.Speed);
            var currentOffset = math.dot(position.Value - motion.InitialPosition, motion.Direction);
            velocity.Linear = motion.Direction * (desiredOffset - currentOffset);

            velocity.Angular = motion.Rotation;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Random random = new Random();

        var job = new PlatformMotionJob { deltaTime = UnityEngine.Time.fixedDeltaTime, random = random };
        var jobHandle = job.ScheduleSingle(this, inputDeps);

        return jobHandle;
    }
}
