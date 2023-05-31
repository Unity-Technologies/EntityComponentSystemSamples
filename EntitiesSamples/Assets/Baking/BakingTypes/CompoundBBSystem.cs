using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Baking.BakingTypes
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct CompoundBBSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CompoundBBComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Add the cleanup component to every entity contributing the bounding boxes.
            var missingCleanupQuery = SystemAPI.QueryBuilder().WithAll<BoundingBox>()
                .WithNone<BoundingBoxCleanup>().Build();
            state.EntityManager.AddComponent<BoundingBoxCleanup>(missingCleanupQuery);

            // Find the parent bounding boxes that have changes in their children and reset their values.
            var changedCBBs = new NativeHashSet<Entity>(1, Allocator.Temp);
            foreach (var (bb, pp) in
                     SystemAPI.Query<RefRO<BoundingBox>, RefRW<BoundingBoxCleanup>>()
                         .WithAll<Changes>())
            {
                var parent = bb.ValueRO.Parent;
                changedCBBs.Add(parent);

                var previousParent = pp.ValueRO.PreviousParent;
                if (previousParent != Entity.Null && previousParent != parent)
                {
                    // If this entity has been re-parented, both the previous and current parent need to be updated.
                    changedCBBs.Add(previousParent);
                }

                pp.ValueRW.PreviousParent = parent;
            }

            // If an entity has been destroyed, only its cleanup component is left. The previous parent needs updating.
            foreach (var pp in
                     SystemAPI.Query<RefRO<BoundingBoxCleanup>>()
                         .WithNone<BoundingBox>())
            {
                var previousParent = pp.ValueRO.PreviousParent;
                if (previousParent != Entity.Null)
                {
                    changedCBBs.Add(previousParent);
                }
            }

            // Destroyed entities are kept alive by their cleanup component, so they have to be explicitly removed.
            var removedEntities = SystemAPI.QueryBuilder().WithAll<BoundingBoxCleanup>()
                .WithNone<BoundingBox>().Build();
            state.EntityManager.RemoveComponent<BoundingBoxCleanup>(removedEntities);

            // Every parent that needs updating has its bounding box reset.
            foreach (var parent in changedCBBs)
            {
                SystemAPI.SetComponent(parent, new CompoundBBComponent()
                {
                    MinBBVertex = new float3(float.MaxValue),
                    MaxBBVertex = new float3(float.MinValue)
                });
            }

            // Calculate the compounded bounding box of all the cubes
            var compoundBBLookup = SystemAPI.GetComponentLookup<CompoundBBComponent>();
            foreach (var bb in
                     SystemAPI.Query<RefRO<BoundingBox>>())
            {
                var parent = bb.ValueRO.Parent;
                if (!changedCBBs.Contains(parent))
                {
                    continue;
                }

                var parentBB = compoundBBLookup.GetRefRW(bb.ValueRO.Parent);
                ref var parentMin = ref parentBB.ValueRW.MinBBVertex;
                ref var parentMax = ref parentBB.ValueRW.MaxBBVertex;
                var min = bb.ValueRO.MinBBVertex;
                var max = bb.ValueRO.MaxBBVertex;

                parentMax.x = math.max(max.x, parentMax.x);
                parentMax.y = math.max(max.y, parentMax.y);
                parentMax.z = math.max(max.z, parentMax.z);
                parentMin.x = math.min(min.x, parentMin.x);
                parentMin.y = math.min(min.y, parentMin.y);
                parentMin.z = math.min(min.z, parentMin.z);
            }
        }
    }
}
