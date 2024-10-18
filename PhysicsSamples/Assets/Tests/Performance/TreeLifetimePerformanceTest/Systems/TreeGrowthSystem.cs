// This system iterates over entities with the TreeTopTag (note: the trunk doesn't grow) and only runs if the TreeFlag
// value equals FlagSettings.TriggerTreeGrowthSystem. The collider size increases and when the growth is done, the
// treeState is set to Default. The TreeLifecycleSystem is responsible for toggling the TreeFlag that will enable this system.
// Note that the entities being modified are static bodies.
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Profiling;
using Unity.Transforms;

namespace Unity.Physics
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    public partial struct TreeGrowthSystem : ISystem
    {
        private static readonly float growthRate = 0.2f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TreeState>();
            state.RequireForUpdate<EnableTreeGrowth>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ProfilerMarker pm = new ProfilerMarker("Profile: TreeGrowthSystem.OnUpdate"); //PROFILE
            pm.Begin(); //PROFILE

            // Condition to run: has a collider, a TreeGrowthTag, treeState = TriggerTreeGrowth
            using var ecb = new EntityCommandBuffer(Allocator.TempJob);
            state.Dependency = new GrowTreeColliderJob
            {
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);

            // Once trees are done growing, remove the TreeGrowthTag component
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);

            pm.End(); //PROFILE
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        // Grow the tree top by increasing the size of the collider
        [BurstCompile]
        public partial struct GrowTreeColliderJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute([ChunkIndexInQuery] int chunkInQueryIndex, Entity entity, ref TreeState treeState,
                ref PhysicsCollider collider, ref PostTransformMatrix postTransformMatrix, EnableTreeGrowth enableTreeGrowth)
            {
                // Note: treeState.Value MUST equal TreeState.States.TriggerTreeGrowthSystem for EnableTreeGrowth to be
                // present, so we aren't checking for it here.

                // this is a tree top identified as ready to grow
                float3 oldSize = 1.0f;
                float3 newSize = 1.0f;
                unsafe
                {
                    // grab the box pointer
                    BoxCollider* bxPtr = (BoxCollider*)collider.ColliderPtr;
                    oldSize = bxPtr->Size;
                    var oldCenter = bxPtr->Center;

                    newSize = oldSize;
                    newSize.y += growthRate;

                    var newCenter = oldCenter;
                    newCenter.y += (growthRate * 0.5f);

                    var boxGeometry = bxPtr->Geometry;
                    boxGeometry.Size = newSize;
                    boxGeometry.Center = newCenter;
                    bxPtr->Geometry = boxGeometry;
                }

                // now tweak the graphical representation of the box
                float3 newScale = newSize / oldSize;
                postTransformMatrix.Value.c0 *= newScale.x;
                postTransformMatrix.Value.c1 *= newScale.y;
                postTransformMatrix.Value.c2 *= newScale.z;

                // When growth is done, set treeState to make the tree static again
                treeState.Value = TreeState.States.Default;

                // Component should only be enabled on entities that are timed to grow, so disable once growth done
                ECB.SetComponentEnabled<EnableTreeGrowth>(chunkInQueryIndex, entity, false);
            }
        }
    }
}
