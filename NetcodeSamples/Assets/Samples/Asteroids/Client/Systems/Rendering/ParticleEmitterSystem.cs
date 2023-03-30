using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = UnityEngine.Random;
using Unity.NetCode;
using Unity.Rendering;
using Unity.Burst;

namespace Asteroids.Client
{
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateBefore(typeof(ParticleUpdateSystemGroup))]
    [BurstCompile]
    public partial struct ParticleEmitterSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ParticleEmitterComponentData>()
                .WithNone<Particle>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new EmitParticleJob
            {
                commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                deltaTime = SystemAPI.Time.DeltaTime
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
        [WithNone(typeof(Particle))]
        [BurstCompile]
        partial struct EmitParticleJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter commandBuffer;
            public float deltaTime;

            public void Execute(Entity entity, [EntityIndexInChunk] int entityIndexInChunk, in ParticleEmitterComponentData emitter, in LocalTransform transform)

            {
                if (emitter.active == 0)
                    return;
                int particles = (int) (deltaTime * emitter.particlesPerSecond + 0.5f);
                if (particles == 0)
                    return;

                float2 spawnOffset =
                    math.mul(transform.Rotation, new float3(emitter.spawnOffset, 0)).xy;


                bool colorTrans = math.any(emitter.startColor != emitter.endColor);
                bool sizeTrans = emitter.startLength != emitter.endLength ||
                                 emitter.startWidth != emitter.endWidth;
                // Create the first particle, then instantiate the rest based on its value
                var particle = commandBuffer.Instantiate(entityIndexInChunk, emitter.particlePrefab);
                commandBuffer.AddComponent(entityIndexInChunk, particle, default(Particle));
                commandBuffer.AddComponent(entityIndexInChunk, particle, new URPMaterialPropertyBaseColor {Value = emitter.startColor});
                commandBuffer.AddComponent(entityIndexInChunk, particle, new ParticleAge(emitter.particleLifetime));
                commandBuffer.AddComponent(entityIndexInChunk, particle, emitter);
                // Set initial data
                commandBuffer.AddComponent(entityIndexInChunk, particle, new ParticleVelocity());

                commandBuffer.SetComponent(entityIndexInChunk, particle,
                    LocalTransform.FromPositionRotation(transform.Position + new float3(spawnOffset, 0), transform.Rotation));
                commandBuffer.SetComponent(entityIndexInChunk, particle, new PostTransformMatrix {Value = float4x4.Scale(emitter.startWidth, emitter.startWidth + emitter.startLength, emitter.startWidth)});

                if (colorTrans)
                    commandBuffer.AddComponent(entityIndexInChunk, particle,
                        new ParticleColorTransition(emitter.startColor,
                            emitter.endColor));
                if (sizeTrans)
                    commandBuffer.AddComponent(entityIndexInChunk, particle,
                        new ParticleSizeTransition(emitter.startLength,
                            emitter.endLength, emitter.startWidth,
                            emitter.endWidth));
                if (particles > 1)
                {
                    for (int i = 1; i < particles; ++i)
                        commandBuffer.Instantiate(entityIndexInChunk, particle);
                }
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateBefore(typeof(ParticleUpdateSystemGroup))]
    [UpdateAfter(typeof(ParticleEmitterSystem))]
    [BurstCompile]
    public partial struct ParticleInitializeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<Particle, ParticleEmitterComponentData>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new InitializeParticleJob
            {
                commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                rand = new Unity.Mathematics.Random((uint)NetworkTimeSystem.TimestampMS)
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
        [WithAll(typeof(Particle))]
        [BurstCompile]
        partial struct InitializeParticleJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter commandBuffer;
            public Unity.Mathematics.Random rand;
            public void Execute(Entity entity, [EntityIndexInChunk] int entityIndexInChunk,

                ref LocalTransform transform, ref ParticleVelocity velocity,

                in ParticleEmitterComponentData emitter)
            {
                var curRand = new Unity.Mathematics.Random(rand.NextUInt() + (uint)entityIndexInChunk);

                transform.Rotation = math.mul(transform.Rotation, quaternion.RotateZ(math.radians(curRand.NextFloat(-emitter.angleSpread,
                    emitter.angleSpread))));

                float particleVelocity = emitter.velocityBase +
                                         curRand.NextFloat(0, emitter.velocityRandom);
                float3 particleDir = new float3(0, particleVelocity, 0);

                velocity.velocity += math.mul(transform.Rotation, particleDir).xy;

                transform.Position.x += curRand.NextFloat(-emitter.spawnSpread, emitter.spawnSpread);
                transform.Position.y += curRand.NextFloat(-emitter.spawnSpread, emitter.spawnSpread);

                commandBuffer.RemoveComponent<ParticleEmitterComponentData>(entityIndexInChunk, entity);
            }
        }
    }
}
