using System.Collections;
using System.Collections.Generic;
using System.Xml.Schema;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Samples.FixedTimestepSystem
{
    // This system updates all entities in the scene with both a RotationSpeed and Rotation component.
    public class MoveProjectilesSystem : JobComponentSystem
    {
        [BurstCompile]
        struct MoveProjectileJob : IJobForEachWithEntity<ProjectileSpawnTime, Translation>
        {
            public EntityCommandBuffer.Concurrent Commands;
            public float TimeSinceLoad;
            public float ProjectileSpeed;

            public void Execute(Entity entity, int index, [ReadOnly] ref ProjectileSpawnTime spawnTime, ref Translation translation)
            {
                float aliveTime = (TimeSinceLoad - spawnTime.SpawnTime);
                if (aliveTime > 5.0f)
                {
                    Commands.DestroyEntity(index, entity);
                }
                translation.Value.x = aliveTime * ProjectileSpeed;
            }
        }

        private BeginSimulationEntityCommandBufferSystem m_beginSimEcbSystem;
        protected override void OnCreateManager()
        {
            m_beginSimEcbSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            var jobHandle = new MoveProjectileJob()
            {
                Commands = m_beginSimEcbSystem.CreateCommandBuffer().ToConcurrent(),
                TimeSinceLoad = Time.timeSinceLevelLoad,
                ProjectileSpeed = 5.0f,
            }.Schedule(this, inputDependencies);
            m_beginSimEcbSystem.AddJobHandleForProducer(jobHandle);

            return jobHandle;
        }
    }
}
