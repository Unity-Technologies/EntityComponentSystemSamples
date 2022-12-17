using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.Profiling;
using UnityEngine.SocialPlatforms;

namespace Samples.Boids
{
    [BurstCompile]
    public partial struct BoidSchoolSpawnSystem : ISystem
    {
        public ComponentLookup<LocalToWorld> LocalToWorldLookup;
        private EntityQuery SchoolQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            SchoolQuery = SystemAPI.QueryBuilder().WithAll<BoidSchool, LocalToWorld>().Build();
            LocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>();
            
            state.RequireForUpdate(SchoolQuery);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            LocalToWorldLookup.Update(ref state);

            var boidSchools = SchoolQuery.ToComponentDataArray<BoidSchool>(Allocator.Temp);
            var localToWorlds = SchoolQuery.ToComponentDataArray<LocalToWorld>(Allocator.Temp);
            var schoolEntities = SchoolQuery.ToEntityArray(Allocator.Temp);

            var world = state.WorldUnmanaged;
            
            for (int i = 0; i < boidSchools.Length; i++)
            {
                var boidEntities = CollectionHelper.CreateNativeArray<Entity, RewindableAllocator>(boidSchools[i].Count, ref world.UpdateAllocator);
                state.EntityManager.Instantiate(boidSchools[i].Prefab, boidEntities);

                var job = new SetBoidLocalToWorld
                {
                    LocalToWorldFromEntity = LocalToWorldLookup,
                    Entities = boidEntities,
                    Center = localToWorlds[i].Position,
                    Radius = boidSchools[i].InitialRadius
                };
                state.Dependency = job.Schedule(boidSchools[i].Count, 1000, state.Dependency);
            }
            
            state.EntityManager.DestroyEntity(schoolEntities);
        }

        [BurstCompile]
        struct SetBoidLocalToWorld : IJobParallelFor
        {
            [NativeDisableContainerSafetyRestriction]
            [NativeDisableParallelForRestriction]
            public ComponentLookup<LocalToWorld> LocalToWorldFromEntity;

            public NativeArray<Entity> Entities;
            public float3 Center;
            public float Radius;

            public void Execute(int i)
            {
                var entity = Entities[i];
                var random = new Random(((uint)(entity.Index + i + 1) * 0x9F6ABC1));
                var dir = math.normalizesafe(random.NextFloat3() - new float3(0.5f, 0.5f, 0.5f));
                var pos = Center + (dir * Radius);
                var localToWorld = new LocalToWorld
                {
                    Value = float4x4.TRS(pos, quaternion.LookRotationSafe(dir, math.up()), new float3(1.0f, 1.0f, 1.0f))
                };
                LocalToWorldFromEntity[entity] = localToWorld;
            }
        }


    }
}
