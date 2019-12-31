using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.Profiling;

namespace Samples.Boids
{
    public struct BoidSchool : IComponentData
    {
        public Entity Prefab;
        public float InitialRadius;
        public int Count;
    }

    public class BoidSchoolSpawnSystem : JobComponentSystem
    {
        [BurstCompile]
        struct SetBoidLocalToWorld : IJobParallelFor
        {
            [NativeDisableContainerSafetyRestriction]
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<LocalToWorld> LocalToWorldFromEntity;
            
            public NativeArray<Entity> Entities;
            public float3 Center;
            public float Radius;
            
            public void Execute(int i)
            {
                var entity = Entities[i];
                var random = new Random(((uint)(entity.Index + i + 1) * 0x9F6ABC1));
                var dir = math.normalizesafe(random.NextFloat3() - new float3(0.5f,0.5f,0.5f));
                var pos = Center + (dir * Radius);
                var localToWorld = new LocalToWorld
                {
                  Value = float4x4.TRS(pos, quaternion.LookRotationSafe(dir, math.up()), new float3(1.0f, 1.0f, 1.0f))
                };
                LocalToWorldFromEntity[entity] = localToWorld;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Entities.WithStructuralChanges().ForEach((Entity entity, int entityInQueryIndex, in BoidSchool boidSchool, in LocalToWorld boidSchoolLocalToWorld) =>
            {
                var boidEntities = new NativeArray<Entity>(boidSchool.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                
                Profiler.BeginSample("Instantiate");
                EntityManager.Instantiate(boidSchool.Prefab, boidEntities);
                Profiler.EndSample();
                
                var localToWorldFromEntity = GetComponentDataFromEntity<LocalToWorld>();
                var setBoidLocalToWorldJob = new SetBoidLocalToWorld
                {
                    LocalToWorldFromEntity = localToWorldFromEntity,
                    Entities = boidEntities,
                    Center = boidSchoolLocalToWorld.Position,
                    Radius = boidSchool.InitialRadius
                };
                inputDeps = setBoidLocalToWorldJob.Schedule(boidSchool.Count, 64, inputDeps);
                inputDeps = boidEntities.Dispose(inputDeps);
                
                EntityManager.DestroyEntity(entity);
            }).Run();
            
            return inputDeps;
        }
    }
}
