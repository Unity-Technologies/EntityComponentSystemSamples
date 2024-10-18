// This system iterates over all tree root entities and spawns a new tree at their original location if their state is
// LifeCycleStates.TransitionToInsert.
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Transforms;

namespace Unity.Physics
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct TreeRegrowSystem : ISystem
    {
        [BurstCompile]
        [WithAll(typeof(TreeState))]
        partial struct TreeRegrowJob : IJobEntity
        {
            [NativeSetThreadIndex] private int m_ThreadIndex;
            public Entity TreePrefab;
            public TreeSpawnerComponent Spawner;
            public float TimeStep;
            public EntityCommandBuffer.ParallelWriter ECB;

            void Execute(in Entity entity, in TreeComponent treeComponent, in LocalTransform localTransform, [ChunkIndexInQuery] int chunkIndex)
            {
                if (treeComponent.LifeCycleTracker != LifeCycleStates.TransitionToInsert)
                    return;

                var newTreeEntity = ECB.Instantiate(chunkIndex, TreePrefab);

                var random = new Random((uint)entity.GetHashCode());
                var growTime = random.NextFloat(1, Spawner.MaxGrowTime);
                var deadTime = random.NextFloat(0, Spawner.MaxDeadTime);
                var regrowDelay = random.NextFloat(1, Spawner.ReGrowDelay);

                var growInCounts = growTime / TimeStep;
                var deadInCounts = deadTime / TimeStep;
                var regrowInCounts = regrowDelay / TimeStep;

                // Reset TreeComponent data for the next lifecycle - reusing the previous initialization values
                var newTreeComponent = new TreeComponent()
                {
                    SpawningPosition = treeComponent.SpawningPosition,
                    GrowTime = growTime,
                    DeadTime = deadTime,

                    GrowTimer = (int)growInCounts,
                    DeathTimer = (int)deadInCounts,
                    RegrowTimer = (int)regrowInCounts,
                    LifeCycleTracker = LifeCycleStates.IsGrowing
                };

                ECB.AddComponent(chunkIndex, newTreeEntity, newTreeComponent);
                ECB.AddComponent(chunkIndex, newTreeEntity, new TreeState { Value = TreeState.States.Default });
                ECB.AddComponent(chunkIndex, newTreeEntity, new TempIntermediateTreeSpawningTag());
                ECB.DestroyEntity(chunkIndex, entity); // Destroys remaining, dead Tree instance

                // Update the new Tree instance:
                ECB.SetComponent(chunkIndex, newTreeEntity,  new LocalTransform
                {
                    Position =  treeComponent.SpawningPosition,
                    Scale = localTransform.Scale,
                    Rotation = localTransform.Rotation
                });
            }
        }

        [BurstCompile]
        [WithAll(typeof(TempIntermediateTreeSpawningTag))]
        partial struct TreePositioningJob : IJobEntity
        {
            [ReadOnly]
            public BufferLookup<LinkedEntityGroup> LinkedEntityGroupLookup;
            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransformLookup;
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(in Entity entity, in LocalTransform localTransform, [ChunkIndexInQuery] int chunkIndex)
            {
                var leg = LinkedEntityGroupLookup[entity];
                for (var j = 1; j < leg.Length; j++)
                {
                    var childEntity = leg[j].Value;

                    if (LocalTransformLookup.HasComponent(childEntity))
                    {
                        var childLocalTransform = LocalTransformLookup[childEntity];
                        var lt = new LocalTransform
                        {
                            Position = localTransform.Position + childLocalTransform.Position,
                            Scale = childLocalTransform.Scale,
                            Rotation = childLocalTransform.Rotation
                        };
                        ECB.SetComponent(chunkIndex, childEntity, lt);
                    }
                }

                ECB.RemoveComponent<TempIntermediateTreeSpawningTag>(chunkIndex, entity);
            }
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TreeComponent>();
            state.RequireForUpdate<TreeState>();
            state.RequireForUpdate<TreeSpawnerComponent>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ProfilerMarker pm = new ProfilerMarker("Profile: TreeRegrowSystem.OnUpdate"); //PROFILE
            pm.Begin(); //PROFILE

            var spawner = SystemAPI.GetSingleton<TreeSpawnerComponent>();
            Entity treePrefab = spawner.TreeEntity;

            if (treePrefab == Entity.Null)
            {
                pm.End();
                return;
            }

            var timeStep = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            var treeGrowJob = new TreeRegrowJob
            {
                TreePrefab = treePrefab,
                Spawner = spawner,
                TimeStep = timeStep,
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);

            treeGrowJob.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new TreePositioningJob
            {
                LinkedEntityGroupLookup = SystemAPI.GetBufferLookup<LinkedEntityGroup>(isReadOnly: true),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true),
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);

            pm.End(); //PROFILE
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
