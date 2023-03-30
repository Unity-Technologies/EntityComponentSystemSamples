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

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial struct ExplosionCountdownSystem : ISystem
{
    private ComponentLookup<LocalTransform> m_LocalTransformLookup;

    [BurstCompile]
    private partial struct ExplosionCountdown_TickJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransforms;

        private void Execute(Entity entity, ref ExplosionCountdown explosion)
        {
            explosion.Countdown--;
            bool bang = explosion.Countdown <= 0;
            if (bang && !explosion.Source.Equals(Entity.Null))
            {
                explosion.Center = LocalTransforms[explosion.Source].Position;
            }
        }
    }

    [BurstCompile]
    private partial struct IJobEntity_ExplosionCountdown_Bang : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter CommandBufferParallel;
        public float DeltaTime;
        public float3 Up;

        private void Execute([ChunkIndexInQuery] int chunkInQueryIndex, Entity entity, ref ExplosionCountdown explosion, ref PhysicsVelocity velocity, in PhysicsMass mass,
            in PhysicsCollider collider, in LocalTransform localTransform)
        {
            if (0 < explosion.Countdown) return;

            velocity.ApplyExplosionForce(mass, collider, localTransform.Position, localTransform.Rotation,
                explosion.Force, explosion.Center, 0, DeltaTime, Up);

            CommandBufferParallel.RemoveComponent<ExplosionCountdown>(chunkInQueryIndex, entity);
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        m_LocalTransformLookup = state.GetComponentLookup<LocalTransform>(true);

        state.RequireForUpdate<ExplosionCountdown>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        m_LocalTransformLookup.Update(ref state);

        state.Dependency = new ExplosionCountdown_TickJob
        {
            LocalTransforms = m_LocalTransformLookup,
        }.ScheduleParallel(state.Dependency);

        state.Dependency = new IJobEntity_ExplosionCountdown_Bang
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            Up = math.up(),
            CommandBufferParallel = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
        }.Schedule(state.Dependency);
    }
}
