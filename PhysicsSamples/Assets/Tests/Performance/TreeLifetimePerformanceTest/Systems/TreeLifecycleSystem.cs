//#define DEBUG_TREE_GROW_LOGGING

// This system iterates over all the entities with a TreeComponent (aka: the tree root / prefab). The TreeComponent
// counts down timers that track where a tree is in its lifecycle, where a tree transitions from:
// growing > dead > deleted > regrown and then the cycle repeats. The system counts down the timers. The transition between
// lifecycle states is done in other systems. This work generally needs to be done on the child entities in the prefab's
// LinkedEntityGroup, therefore this system sets the TreeFlag on each of these entities to trigger the work in other systems.
// Lifecycle state changes to countdown the next timer are generally done in the other systems so that we can be sure
// the work is done. All data modified is written to the EndFixedStepSimulationEntityCommandBufferSystem which runs
// after the PhysicsSystemGroup. This means that some data which is set, won't be processed by another system until the
// next frame
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Physics
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(TreeSpawnerSystem))]
    [UpdateAfter(typeof(TreeRegrowSystem))]
    public partial struct TreeLifecycleSystem : ISystem
    {
#if DEBUG_TREE_GROW_LOGGING
        UnsafeAtomicCounter32 m_GrowingTreesCounter;
        int m_GrowingTreesCount;
#endif

        [BurstCompile]
        private partial struct LifecycleCountdownJob : IJobEntity
        {
            [NativeDisableUnsafePtrRestriction]
#if DEBUG_TREE_GROW_LOGGING
            public UnsafeAtomicCounter32 GrowingTreesCounter;
#endif
            [ReadOnly] public ComponentLookup<TreeState> TreeStateLookup;
            [ReadOnly] public ComponentLookup<TreeTopTag> TreeTopTagLookup;
            public EntityCommandBuffer.ParallelWriter ECB;
            public float GrowTreeProbability;

            // Gather all the entities with the TreeComponent (these will be the prefab tree roots only)
            private void Execute([ChunkIndexInQuery] int chunkInQueryIndex, Entity entity,
                ref TreeComponent treeComponent, in DynamicBuffer<LinkedEntityGroup> group)
            {
                var rootTreeState = TreeStateLookup[entity];

                switch (treeComponent.LifeCycleTracker)
                {
                    default:
                    case (LifeCycleStates.IsGrowing): // Count down GrowTimer
                        treeComponent.GrowTimer--;

                        // dice roll to see if we grow the tree
                        var treeHash = entity.Index * 17 ^ entity.Version * 23;
                        var hash = (uint)treeComponent.GrowTimer * 327 ^ (uint)treeComponent.GrowTime * 1571;
                        var seed = math.max(1, hash ^ (uint)treeHash);
                        var random = new Random(seed);
                        var p = random.NextFloat(1.0f);

                        if (p <= GrowTreeProbability)
                        {
#if DEBUG_TREE_GROW_LOGGING
                            GrowingTreesCounter.Add(1);
#endif

                            if (group.Length > 1)
                            {
                                // Loop through the prefab LinkedEntityGroup entities
                                for (var i = 1; i < group.Length; i++) //skip root
                                {
                                    var childEntity = group[i].Value;

                                    // Check if the entity is a tree top
                                    var isTreeTop = TreeTopTagLookup.HasComponent(childEntity);
                                    var childTreeState = TreeStateLookup[childEntity];

                                    // For a tree top, toggle flag from Default > TriggerTreeGrowthSystem
                                    if (isTreeTop && childTreeState.Value == TreeState.States.Default)
                                    {
                                        childTreeState.Value = TreeState.States.TriggerTreeGrowthSystem;
                                        ECB.SetComponent(chunkInQueryIndex, childEntity, childTreeState);

                                        // Incremental broadphase needs a smaller collection of entities to work with,
                                        // so add enable the EnableTreeGrowth component for query filtering
                                        ECB.SetComponentEnabled<EnableTreeGrowth>(chunkInQueryIndex, childEntity,
                                            true);
                                    }
                                }
                            }
                        }

                        // Change states when timer expires
                        if (treeComponent.GrowTimer <= 0) treeComponent.LifeCycleTracker = LifeCycleStates.TransitionToDead;
                        break;

                    case (LifeCycleStates.TransitionToDead):
                        // Work is done in TreeDeathSystem
                        if (treeComponent.GrowTimer <= 0) // Verify grow timer has expired
                        {
                            if (group.Length > 1)
                            {
                                bool allWorkDone = true;
                                // Loop through the prefab LinkedEntityGroup entities
                                for (var i = 1; i < group.Length; i++) //start after root
                                {
                                    var childEntity = group[i].Value;

                                    // update flag for all children
                                    var childTreeState = TreeStateLookup[childEntity];
                                    switch (childTreeState.Value)
                                    {
                                        case TreeState.States.Default:
                                        {
                                            // if takes longer than a frame, don't want to overwrite
                                            childTreeState.Value = TreeState.States.TriggerWholeTreeToDynamic;
                                            ECB.SetComponent(chunkInQueryIndex, childEntity, childTreeState);

                                            // Incremental broadphase needs a smaller collection of entities to work with,
                                            // so add enable the EnableTreeDeath component for query filtering
                                            ECB.SetComponentEnabled<EnableTreeDeath>(chunkInQueryIndex, childEntity, true);
                                            break;
                                        }
                                        case TreeState.States.TransitionToDeadDone:
                                        {
                                            // When TreeDeathSystem is done, we will end up here
                                            allWorkDone &= (childTreeState.Value == TreeState.States.TriggerChangeTreeColor);
                                            break;
                                        }
                                        default:
                                            break;
                                    }
                                }
                                // Transition to next state only when all work is done
                                if (allWorkDone)
                                {
                                    // Skip "dead" state and go straight away to deletion if dead time is 0
                                    treeComponent.LifeCycleTracker = treeComponent.DeadTime > 0 ? LifeCycleStates.IsDead : LifeCycleStates.TransitionToDelete;
                                }
                            }
                        }

                        break;

                    case (LifeCycleStates.IsDead): // Countdown DeathTimer
                        treeComponent.DeathTimer--;

                        // Do nothing but countdown timer and change states when it expires
                        if (treeComponent.DeathTimer <= 0) treeComponent.LifeCycleTracker = LifeCycleStates.TransitionToDelete;
                        break;

                    case (LifeCycleStates.TransitionToDelete):
                        // Transition: delete all entities within the LinkedEntityGroup
                        // TreeDeletionSystem uses this lifecycle state directly. Do nothing.
                        if (rootTreeState.Value != TreeState.States.TriggerDeleteTrunkAndTop) // just in case this slips a frame
                        {
                            rootTreeState.Value = TreeState.States.TriggerDeleteTrunkAndTop;
                            ECB.SetComponent(chunkInQueryIndex, entity, rootTreeState);
                        }

                        // Transition to next state in TreeDeletionSystem
                        break;

                    case (LifeCycleStates.IsRegrown):
                        // Countdown: the time until the tree is respawned
                        treeComponent.RegrowTimer--;

                        if (treeComponent.RegrowTimer <= 0) treeComponent.LifeCycleTracker = LifeCycleStates.TransitionToInsert;
                        break;

                    case (LifeCycleStates.TransitionToInsert):
                        // Transition: respawn the tree
                        // Work is done in the TreeRegrowSystem
                        break;
                }
            }
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TreeComponent>();
            state.RequireForUpdate<TreeSpawnerComponent>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();

#if DEBUG_TREE_GROW_LOGGING
            unsafe
            {
                fixed(int* countPtr = &m_GrowingTreesCount)
                {
                    m_GrowingTreesCounter = new UnsafeAtomicCounter32(countPtr);
                }
            }
#endif
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var treeStateLookup = SystemAPI.GetComponentLookup<TreeState>(isReadOnly: true);
            var treeTopTagLookup = SystemAPI.GetComponentLookup<TreeTopTag>(isReadOnly: true);

#if DEBUG_TREE_GROW_LOGGING
            UnityEngine.Debug.Log($"TreeLifecycleSystem.OnUpdate: Growing trees count: {m_GrowingTreesCount}");
            m_GrowingTreesCount = 0;
#endif

            var spawner = SystemAPI.GetSingleton<TreeSpawnerComponent>();

            // Countdown the timers
            state.Dependency = new LifecycleCountdownJob
            {
#if DEBUG_TREE_GROW_LOGGING
                GrowingTreesCounter = m_GrowingTreesCounter,
#endif
                TreeStateLookup = treeStateLookup,
                TreeTopTagLookup = treeTopTagLookup,
                ECB = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                GrowTreeProbability = spawner.TreeGrowProbability
            }.ScheduleParallel(state.Dependency);
        }
    }
}
