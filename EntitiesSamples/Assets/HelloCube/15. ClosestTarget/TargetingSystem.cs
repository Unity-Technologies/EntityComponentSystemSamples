using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Transforms;

namespace HelloCube.ClosestTarget
{
    public partial struct TargetingSystem : ISystem
    {
        public enum SpatialPartitioningType
        {
            None,
            Simple,
            KDTree,
        }

        static NativeArray<ProfilerMarker> s_ProfilerMarkers;

        public void OnCreate(ref SystemState state)
        {
            s_ProfilerMarkers = new NativeArray<ProfilerMarker>(3, Allocator.Persistent);
            s_ProfilerMarkers[0] = new(nameof(TargetingSystem) + "." + SpatialPartitioningType.None);
            s_ProfilerMarkers[1] = new(nameof(TargetingSystem) + "." + SpatialPartitioningType.Simple);
            s_ProfilerMarkers[2] = new(nameof(TargetingSystem) + "." + SpatialPartitioningType.KDTree);

            state.RequireForUpdate<Settings>();
            state.RequireForUpdate<ExecuteClosestTarget>();
        }

        public void OnDestroy(ref SystemState state)
        {
            s_ProfilerMarkers.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            var targetQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform>().WithNone<Target, Settings>().Build();
            var kdQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform, Target>().Build();

            var spatialPartitioningType = SystemAPI.GetSingleton<Settings>().SpatialPartitioning;

            using var profileMarker = s_ProfilerMarkers[(int)spatialPartitioningType].Auto();

            var targetEntities = targetQuery.ToEntityArray(state.WorldUpdateAllocator);
            var targetTransforms =
                targetQuery.ToComponentDataArray<LocalTransform>(state.WorldUpdateAllocator);

            switch (spatialPartitioningType)
            {
                case SpatialPartitioningType.None:
                {
                    var noPartitioning = new NoPartitioning
                        { TargetEntities = targetEntities, TargetTransforms = targetTransforms };
                    state.Dependency = noPartitioning.ScheduleParallel(state.Dependency);
                    break;
                }
                case SpatialPartitioningType.Simple:
                {
                    var positions = CollectionHelper.CreateNativeArray<PositionAndIndex>(targetTransforms.Length,
                        state.WorldUpdateAllocator);

                    for (int i = 0; i < positions.Length; i += 1)
                    {
                        positions[i] = new PositionAndIndex
                        {
                            Index = i,
                            Position = targetTransforms[i].Position.xz
                        };
                    }

                    state.Dependency = positions.SortJob(new AxisXComparer()).Schedule(state.Dependency);

                    var simple = new SimplePartitioning { TargetEntities = targetEntities, Positions = positions };
                    state.Dependency = simple.ScheduleParallel(state.Dependency);
                    break;
                }
                case SpatialPartitioningType.KDTree:
                {
                    var tree = new KDTree(targetEntities.Length, Allocator.TempJob, 64);

                    // init KD tree
                    for (int i = 0; i < targetEntities.Length; i += 1)
                    {
                        // NOTE - the first parameter is ignored, only the index matters
                        tree.AddEntry(i, targetTransforms[i].Position);
                    }

                    state.Dependency = tree.BuildTree(targetEntities.Length, state.Dependency);

                    var queryKdTree = new QueryKDTree
                    {
                        Tree = tree,
                        TargetEntities = targetEntities,
                        Scratch = default,
                        TargetHandle = SystemAPI.GetComponentTypeHandle<Target>(),
                        LocalTransformHandle = SystemAPI.GetComponentTypeHandle<LocalTransform>(true)
                    };
                    state.Dependency = queryKdTree.ScheduleParallel(kdQuery, state.Dependency);

                    state.Dependency.Complete();
                    tree.Dispose();
                    break;
                }
            }

            state.Dependency.Complete();
        }
    }

    [BurstCompile]
    public struct QueryKDTree : IJobChunk
    {
        [ReadOnly] public NativeArray<Entity> TargetEntities;
        public PerThreadWorkingMemory Scratch;
        public KDTree Tree;

        public ComponentTypeHandle<Target> TargetHandle;
        [ReadOnly] public ComponentTypeHandle<LocalTransform> LocalTransformHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
            in v128 chunkEnabledMask)
        {
            var targets = chunk.GetNativeArray(ref TargetHandle);
            var transforms = chunk.GetNativeArray(ref LocalTransformHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                if (!Scratch.Neighbours.IsCreated)
                {
                    Scratch.Neighbours = new NativePriorityHeap<KDTree.Neighbour>(1, Allocator.Temp);
                }

                Scratch.Neighbours.Clear();
                Tree.GetEntriesInRangeWithHeap(unfilteredChunkIndex, transforms[i].Position, float.MaxValue,
                    ref Scratch.Neighbours);
                var nearest = Scratch.Neighbours.Peek().index;
                targets[i] = new Target { Value = TargetEntities[nearest] };
            }
        }
    }

    [BurstCompile]
    public partial struct SimplePartitioning : IJobEntity
    {
        [ReadOnly] public NativeArray<Entity> TargetEntities;
        [ReadOnly] public NativeArray<PositionAndIndex> Positions;

        public void Execute(ref Target target, in LocalTransform translation)
        {
            var ownpos = new PositionAndIndex { Position = translation.Position.xz };
            var index = Positions.BinarySearch(ownpos, new AxisXComparer());
            if (index < 0) index = ~index;
            if (index >= Positions.Length) index = Positions.Length - 1;

            var closestDistSq = math.distancesq(ownpos.Position, Positions[index].Position);
            var closestEntity = index;

            Search(index + 1, Positions.Length, +1, ref closestDistSq, ref closestEntity, ownpos);
            Search(index - 1, -1, -1, ref closestDistSq, ref closestEntity, ownpos);

            target.Value = TargetEntities[Positions[closestEntity].Index];
        }

        void Search(int startIndex, int endIndex, int step, ref float closestDistSqRef, ref int closestEntityRef,
            PositionAndIndex ownpos)
        {
            for (int i = startIndex; i != endIndex; i += step)
            {
                var xdiff = ownpos.Position.x - Positions[i].Position.x;
                xdiff *= xdiff;

                if (xdiff > closestDistSqRef) break;

                var distSq = math.distancesq(Positions[i].Position, ownpos.Position);

                if (distSq < closestDistSqRef)
                {
                    closestDistSqRef = distSq;
                    closestEntityRef = i;
                }
            }
        }
    }


    [BurstCompile]
    public partial struct NoPartitioning : IJobEntity
    {
        [ReadOnly] public NativeArray<LocalTransform> TargetTransforms;

        [ReadOnly] public NativeArray<Entity> TargetEntities;

        public void Execute(ref Target target, in LocalTransform translation)
        {
            var closestDistSq = float.MaxValue;
            var closestEntity = Entity.Null;

            for (int i = 0; i < TargetTransforms.Length; i += 1)
            {
                var distSq = math.distancesq(TargetTransforms[i].Position, translation.Position);
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    closestEntity = TargetEntities[i];
                }
            }

            target.Value = closestEntity;
        }
    }

    public struct AxisXComparer : IComparer<PositionAndIndex>
    {
        public int Compare(PositionAndIndex a, PositionAndIndex b)
        {
            return a.Position.x.CompareTo(b.Position.x);
        }
    }

    public struct PositionAndIndex
    {
        public int Index;
        public float2 Position;
    }

    public struct PerThreadWorkingMemory
    {
        [NativeDisableContainerSafetyRestriction]
        public NativePriorityHeap<KDTree.Neighbour> Neighbours;
    }
}
