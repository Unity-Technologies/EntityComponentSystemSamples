// This system uses the TreeFlag value TriggerWholeTreeToDynamic on the entities with TreeTopTag and TreeTrunkTag
// components to signal that the root tree TreeComponent is in the LifeCycleStates.TransitionToDead state. When a tree
// transitions to dead, all children entities of the prefab with a collider will:
// - change from static to dynamic and
// - the collision filter is modified
// - the TreeFlag value is changed from TriggerWholeTreeToDynamic to TriggerChangeTreeColor.
// Note: what the collision filter is changed to makes a difference to the performance of the test:
// - Best performance: don't modify the collision filter at all
// - Good performance: modify the collision filter to collide only with other dead trees (bitshift 8) [Recommended]
// - Poor performance: modify the collision filter to collide with all trees (bitshift 7)
// While changing the collision filter does test the BVH building, the resulting collisions for high tree density on a
// large map have a large impact on simulation and the frame rate
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Profiling;

namespace Unity.Physics
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    public partial struct TreeDeathSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TreeState>();
            state.RequireForUpdate<TreeSpawnerComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ProfilerMarker pm = new ProfilerMarker("Profile: TreeDeathSystem.OnUpdate"); //PROFILE
            pm.Begin(); //PROFILE

            var spawner = SystemAPI.GetSingleton<TreeSpawnerComponent>();
            if (spawner.MaxDeadTime > 0)
            {
                // Make both the tree top and tree trunk dynamic
                using var ecb = new EntityCommandBuffer(Allocator.TempJob);
                var makeTreesDynamicJob = new MakeWholeTreeDynamicJob
                {
                    ECB = ecb.AsParallelWriter(),
                }.ScheduleParallel(state.Dependency);

                makeTreesDynamicJob.Complete();
                ecb.Playback(state.EntityManager);
            }

            pm.End(); //PROFILE
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        // Make both TreeTop and TreeTrunk bodies dynamic and update the collision filter
        [BurstCompile]
        internal partial struct MakeWholeTreeDynamicJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute([ChunkIndexInQuery] int chunkInQueryIndex, Entity entity, ref TreeState treeState,
                PhysicsCollider collider, EnableTreeDeath enableTreeDeath)
            {
                // Note: treeState.Value MUST equal TreeState.States.TriggerWholeTreeToDynamic for EnableTreeDeath to be
                // present, so we aren't checking for it here.

                // Make the body dynamic
                var velocity = new PhysicsVelocity
                {
                    Linear = float3.zero,
                    Angular = float3.zero
                };
                ECB.AddComponent(chunkInQueryIndex, entity, velocity);

                var damping = new PhysicsDamping
                {
                    Linear = 0.0f,
                    Angular = 0.05f
                };
                ECB.AddComponent(chunkInQueryIndex, entity, damping);

                var mass = PhysicsMass.CreateDynamic(collider.MassProperties, 1.0f);
                ECB.AddComponent(chunkInQueryIndex, entity, mass);

                // Update the collision filter to collide with other dead trees
                var filter = collider.Value.Value.GetCollisionFilter();
                filter.CollidesWith ^= (1 << 7);  //toggle bit so it collides with everything
                var newFilter = new CollisionFilter
                {
                    BelongsTo = 256, // now belongs to DeadTrees layer
                    CollidesWith = filter.CollidesWith,
                    GroupIndex = filter.GroupIndex
                };
                collider.Value.Value.SetCollisionFilter(newFilter);
                ECB.SetComponent(chunkInQueryIndex, entity, collider);

                treeState.Value = TreeState.States.TriggerChangeTreeColor;
                ECB.SetComponent(chunkInQueryIndex, entity, treeState);

                // Component should only be enabled on entities that are timed to die, so disable once death done
                ECB.SetComponentEnabled<EnableTreeDeath>(chunkInQueryIndex, entity, false);
            }
        }
    }
}
