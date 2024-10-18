// This system iterates overall entities with the TreeComponent and TreeFlag components (the tree root / prefab) and
// only runs when the state is equal to LifeCycleStates.TransitionToDelete. Every child entity in the prefab
// LinkedEntityGroup is destroyed. The tree root persists. After all the child entities are removed, the TreeRootState
// is set to Default (this is so if the system doesn't run right away, that state isn't set again), and the lifecycle
// state is set to LifeCycleStates.IsRegrown
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Profiling;

namespace Unity.Physics
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    [UpdateAfter(typeof(TreeDeathSystem))]
    public partial struct TreeDeletionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TreeComponent>();
            state.RequireForUpdate<TreeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ProfilerMarker pm = new ProfilerMarker("Profile: TreeDeletionSystem.OnUpdate"); //PROFILE
            pm.Begin(); //PROFILE

            using var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var deleteHandle = new DeleteTreeJob()
            {
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
            deleteHandle.Complete();

            ecb.Playback(state.EntityManager);

            pm.End(); //PROFILE
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public partial struct DeleteTreeJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute([ChunkIndexInQuery] int chunkInQueryIndex, Entity entity, ref TreeState treeRootState,
                ref TreeComponent treeComponent, ref DynamicBuffer<LinkedEntityGroup> group)
            {
                if (treeComponent.LifeCycleTracker != LifeCycleStates.TransitionToDelete)
                    return;

                // delete child entities
                for (var j = 1; j < group.Length; j++)
                {
                    var childEntity = group[j].Value;
                    ECB.DestroyEntity(chunkInQueryIndex, childEntity);
                }

                // clear LinkedEntityGroup to remove deleted entries
                // Note: 0 is the root entity and we can safely remove it as well since we deleted all the children
                // and won't need the LinkedEntityGroup anymore.
                group.Clear();

                // Update so no longer flagged for deletion in TreeLifetimeSystem
                treeRootState.Value = TreeState.States.Default;

                // Update the TreeComponent to the next state
                treeComponent.LifeCycleTracker = LifeCycleStates.IsRegrown;
            }
        }
    }
}
