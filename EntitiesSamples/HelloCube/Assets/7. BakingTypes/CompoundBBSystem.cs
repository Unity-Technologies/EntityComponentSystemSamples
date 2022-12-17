using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace HelloCube.BakingTypes
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct CompoundBBSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Ensures that the system is only run when there are new changes
            state.RequireForUpdate<BoundingBoxComponent>();
            state.RequireForUpdate<ChangesComponent>();
            state.RequireForUpdate<CompoundBBComponent>();
            state.RequireForUpdate<Execute>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Find the parent bounding boxes that have changes in their children and reset their values
            var changedCBBs = new NativeHashSet<Entity>(1, Allocator.Temp);
            foreach (var bb in SystemAPI.Query<RefRO<BoundingBoxComponent>>().WithAll<ChangesComponent>())
            {
                var parent = bb.ValueRO.Parent;
                changedCBBs.Add(parent);
                SystemAPI.SetComponent(parent, new CompoundBBComponent()
                {
                    MinBBVertex = new float3(float.MaxValue),
                    MaxBBVertex = new float3(float.MinValue)
                });
            }

            // Calculate the compounded bounding box of all the cubes
            var componentLookup = SystemAPI.GetComponentLookup<CompoundBBComponent>();
            foreach (var bb in SystemAPI.Query<RefRO<BoundingBoxComponent>>())
            {
                var parent = bb.ValueRO.Parent;
                if (!changedCBBs.Contains(parent))
                {
                    continue;
                }

                var parentBB = componentLookup.GetRefRW(bb.ValueRO.Parent, false);
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
