using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;

public struct ExplosionCountdown : IComponentData
{
    public Entity Source;
    public int Countdown;
    public float3 Center;
    public float Force;
}

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial struct ExplosionCountdownSystem : ISystem
{
#if !ENABLE_TRANSFORM_V1
    private ComponentLookup<LocalTransform> m_LocalTransformLookup;
#else
    private ComponentLookup<Translation> m_PositionLookup;
#endif

    [BurstCompile]
    private partial struct IJobEntity_ExplosionCountdown_Tick : IJobEntity
    {
#if !ENABLE_TRANSFORM_V1
        [ReadOnly]
        public ComponentLookup<LocalTransform> LocalTransforms;
#else
        [ReadOnly]
        public ComponentLookup<Translation> Positions;
#endif

        private void Execute(Entity entity, ref ExplosionCountdown explosion)
        {
            explosion.Countdown--;
            bool bang = explosion.Countdown <= 0;
            if (bang && !explosion.Source.Equals(Entity.Null))
            {
#if !ENABLE_TRANSFORM_V1
                explosion.Center = LocalTransforms[explosion.Source].Position;
#else
                explosion.Center = Positions[explosion.Source].Value;
#endif
            }
        }
    }

    [BurstCompile]
    private partial struct IJobEntity_ExplosionCountdown_Bang : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter CommandBufferParallel;
        public float DeltaTime;
        public float3 Up;

#if !ENABLE_TRANSFORM_V1
        private void Execute([ChunkIndexInQuery] int chunkInQueryIndex, Entity entity, ref ExplosionCountdown explosion, ref PhysicsVelocity velocity, in PhysicsMass mass,
            in PhysicsCollider collider, in LocalTransform localTransform)
#else
        private void Execute([ChunkIndexInQuery] int chunkInQueryIndex, Entity entity, ref ExplosionCountdown explosion, ref PhysicsVelocity velocity, in PhysicsMass mass,
            in PhysicsCollider collider, in Translation pos, in Rotation rot)
#endif
        {
            if (0 < explosion.Countdown) return;

#if !ENABLE_TRANSFORM_V1
            velocity.ApplyExplosionForce(mass, collider, localTransform.Position, localTransform.Rotation,
                explosion.Force, explosion.Center, 0, DeltaTime, Up);
#else
            velocity.ApplyExplosionForce(mass, collider, pos.Value, rot.Value,
                explosion.Force, explosion.Center, 0, DeltaTime, Up);
#endif

            CommandBufferParallel.RemoveComponent<ExplosionCountdown>(chunkInQueryIndex, entity);
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
#if !ENABLE_TRANSFORM_V1
        m_LocalTransformLookup = state.GetComponentLookup<LocalTransform>(true);
#else
        m_PositionLookup = state.GetComponentLookup<Translation>(true);
#endif
        state.RequireForUpdate<ExplosionCountdown>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
#if !ENABLE_TRANSFORM_V1
        m_LocalTransformLookup.Update(ref state);
#else
        m_PositionLookup.Update(ref state);
#endif

        state.Dependency = new IJobEntity_ExplosionCountdown_Tick
        {
#if !ENABLE_TRANSFORM_V1
            LocalTransforms = m_LocalTransformLookup,
#else
            Positions = m_PositionLookup
#endif
        }.ScheduleParallel(state.Dependency);

        state.Dependency = new IJobEntity_ExplosionCountdown_Bang
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            Up = math.up(),
            CommandBufferParallel = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
        }.Schedule(state.Dependency);
    }
}
