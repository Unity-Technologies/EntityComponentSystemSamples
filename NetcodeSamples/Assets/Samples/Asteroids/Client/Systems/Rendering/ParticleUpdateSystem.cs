using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Rendering;
using Unity.Burst;

namespace Asteroids.Client
{
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation, WorldSystemFilterFlags.Presentation)]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public class ParticleUpdateSystemGroup : ComponentSystemGroup
    {
    }

    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(ParticleUpdateSystemGroup))]
    [BurstCompile]
    public partial struct ParticleAgeSystem : ISystem
    {
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
            var job = new ParticleAgeJob
            {
                commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                deltaTime = SystemAPI.Time.DeltaTime
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
        [BurstCompile]
        partial struct ParticleAgeJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter commandBuffer;
            public float deltaTime;
            public void Execute(Entity entity, [EntityIndexInChunk] int entityIndexInChunk, ref ParticleAge age)
            {
                age.age += deltaTime;
                if (age.age >= age.maxAge)
                {
                    age.age = age.maxAge;
                    commandBuffer.DestroyEntity(entityIndexInChunk, entity);
                }
            }
        }
    }

    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(ParticleUpdateSystemGroup))]
    [BurstCompile]
    public partial struct ParticleMoveSystem : ISystem
    {
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
            var job = new ParticleMoveJob
            {
                deltaTime = SystemAPI.Time.DeltaTime
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
        [BurstCompile]
        partial struct ParticleMoveJob : IJobEntity
        {
            public float deltaTime;
#if !ENABLE_TRANSFORM_V1
            public void Execute(ref LocalTransform transform, in ParticleVelocity velocity)
            {
                transform.Position.x += velocity.velocity.x * deltaTime;
                transform.Position.y += velocity.velocity.y * deltaTime;
            }
#else
            public void Execute(ref Translation position, in ParticleVelocity velocity)
            {
                position.Value.x += velocity.velocity.x * deltaTime;
                position.Value.y += velocity.velocity.y * deltaTime;
            }
#endif
        }
    }

    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(ParticleAgeSystem))]
    [UpdateInGroup(typeof(ParticleUpdateSystemGroup))]
    [BurstCompile]
    public partial struct ParticleColorTransitionSystem : ISystem
    {
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
            var job = new ParticleColorJob();
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
        [BurstCompile]
        partial struct ParticleColorJob : IJobEntity
        {
            public void Execute(ref URPMaterialPropertyBaseColor color, in ParticleColorTransition colorDiff, in ParticleAge age)
            {
                float colorScale = age.age / age.maxAge;
                color.Value = colorDiff.startColor + (colorDiff.endColor - colorDiff.startColor) * colorScale;
            }
        }
    }

    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(ParticleAgeSystem))]
    [UpdateInGroup(typeof(ParticleUpdateSystemGroup))]
    [BurstCompile]
    public partial struct ParticleSizeTransitionSystem : ISystem
    {
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
            var job = new ParticleSizeJob();
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
        [BurstCompile]
        partial struct ParticleSizeJob : IJobEntity
        {
#if !ENABLE_TRANSFORM_V1
            public void Execute(ref PostTransformScale scaleMatrix, in ParticleSizeTransition size,
                in ParticleAge age)
            {
                float sizeScale = age.age / age.maxAge;
                var particleLength = size.startLength + (size.endLength - size.startLength) * sizeScale;
                var particleWidth = size.startWidth + (size.endWidth - size.startWidth) * sizeScale;
                scaleMatrix.Value = float3x3.Scale(particleWidth, particleWidth + particleLength, particleWidth);
            }
#else
            public void Execute(ref NonUniformScale scale, in ParticleSizeTransition size,
                in ParticleAge age)
            {
                float sizeScale = age.age / age.maxAge;
                var particleLength = size.startLength + (size.endLength - size.startLength) * sizeScale;
                var particleWidth = size.startWidth + (size.endWidth - size.startWidth) * sizeScale;
                scale.Value = new float3(particleWidth, particleWidth + particleLength, particleWidth);
            }
#endif
        }
    }
}
