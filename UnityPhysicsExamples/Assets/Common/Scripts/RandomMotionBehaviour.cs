using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
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
public class RandomMotionBehaviour : MonoBehaviour, IConvertGameObjectToEntity
{
    public float3 Range = new float3(1);

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var length = math.length(Range);
        dstManager.AddComponentData<RandomMotion>(entity, new RandomMotion
        {
            InitialPosition = transform.position,
            DesiredPosition = transform.position,
            Speed = length * 0.001f,
            Tolerance = length * 0.1f,
            Range = Range,
        });
    }
}


[UpdateBefore(typeof(BuildPhysicsWorld))]
public class RandomMotionSystem : JobComponentSystem
{
    EntityQuery m_PhysicsGroup;

    protected override void OnCreate()
    {
        m_PhysicsGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(PhysicsStep), }
        });
    }

    [BurstCompile]
    protected struct RandomMotionJob : IJobForEach<RandomMotion, Translation, PhysicsVelocity, PhysicsMass>
    {
        public Random random;
        public float deltaTime;
        public float3 gravity;

        public void Execute(ref RandomMotion motion, [ReadOnly] ref Translation position, ref PhysicsVelocity velocity, [ReadOnly] ref PhysicsMass mass)
        {
            motion.CurrentTime += deltaTime;

            random.InitState((uint)(motion.CurrentTime * 1000));
            var currentOffset = position.Value - motion.InitialPosition;
            var desiredOffset = motion.DesiredPosition - motion.InitialPosition;
            // If we are close enough to the destination pick a new destination
            if (math.lengthsq(position.Value - motion.DesiredPosition) < motion.Tolerance)
            {
                var min = new float3(-math.abs(motion.Range));
                var max = new float3(math.abs(motion.Range));
                desiredOffset = random.NextFloat3(min, max);
                motion.DesiredPosition = desiredOffset + motion.InitialPosition;
            }
            var offset = desiredOffset - currentOffset;
            // Smoothly change the linear velocity
            velocity.Linear = math.lerp(velocity.Linear, offset, motion.Speed);
            if (mass.InverseMass != 0)
            {
                velocity.Linear -= gravity * deltaTime;
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Random random = new Random();

        var physicsStep = m_PhysicsGroup.GetSingleton<PhysicsStep>();

        var job = new RandomMotionJob()
        {
            gravity = physicsStep.Gravity,
            deltaTime = Time.fixedDeltaTime,
            random = random,
        };
        var jobHandle = job.Schedule(this, inputDeps);

        return jobHandle;
    }
}
