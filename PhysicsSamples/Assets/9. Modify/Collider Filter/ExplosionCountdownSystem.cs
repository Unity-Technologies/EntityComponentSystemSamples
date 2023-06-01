using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine.Serialization;

namespace Modify
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial struct ExplosionCountdownSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ExplosionCountdown>();
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new TickJob
            {
                LocalTransforms = SystemAPI.GetComponentLookup<LocalTransform>(true),
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new BangJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Up = math.up(),
                ECB = SystemAPI
                    .GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        partial struct TickJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransforms;

            private void Execute(ref ExplosionCountdown explosion)
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
        partial struct BangJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            public float DeltaTime;
            public float3 Up;

            private void Execute([ChunkIndexInQuery] int chunkInQueryIndex, Entity entity,
                ref ExplosionCountdown explosion, ref PhysicsVelocity velocity, in PhysicsMass mass,
                in PhysicsCollider collider, in LocalTransform localTransform)
            {
                if (0 < explosion.Countdown) return;

                velocity.ApplyExplosionForce(mass, collider, localTransform.Position, localTransform.Rotation,
                    explosion.Force, explosion.Center, 0, DeltaTime, Up);

                ECB.RemoveComponent<ExplosionCountdown>(chunkInQueryIndex, entity);
            }
        }
    }

    public struct ExplosionCountdown : IComponentData
    {
        public Entity Source;
        public int Countdown;
        public float3 Center;
        public float Force;
    }
}
