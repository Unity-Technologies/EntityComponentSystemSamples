// This code originates from https://github.cds.internal.unity3d.com/andy-bastable/SpatialTree
// Check that repo and ask for permission before using it in other projects

// #define ENABLE_KDTREE_VALIDATION_CHECKS
// #define ENABLE_KDTREE_ANALYTICS

using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

[NativeContainer]
[NativeContainerSupportsMinMaxWriteRestriction]
public unsafe struct KDTree : IDisposable
{
    const int k_MaxLeafSize = 8;
    const int k_MaxUnbalancedDepth = 5;
    const int k_MinLeavesPerWorker = 16;
    const float k_ZeroRadiusEpsilon = 0.00000000001f;

    const uint k_RootNodeIndex = 1;
    const uint k_IsLeafNodeBitFlag = (uint)(1) << 31;
    const uint k_CountBitMask = ~(k_IsLeafNodeBitFlag);

    internal struct Bounds
    {
        public float3 min; // 12
        public float3 max; // 12

        // 24B
    }

    internal struct SphereBounds
    {
        public float3 centre;
        public float radius;

        // 16B
    }

    public struct TreeNode
    {
        public uint count; // 4

        [NoAlias] internal byte* beginPtr; // 8

        internal SphereBounds bounds; // 16;

        int padding; // 4

        // 32B (2 per cacheline)

        public uint Count => count & k_CountBitMask;

        public bool IsLeaf
        {
            get { return (count & k_IsLeafNodeBitFlag) > 0; }
            set { if (value) count |= k_IsLeafNodeBitFlag; }
        }
    }

    internal struct Entry
    {
        internal int index; // 4
        internal float3 position; // 12

        // 16B (4 per cacheline)
    }

    public struct Neighbour : IComparable<Neighbour>
    {
        public int index; // 4
        public float distSq; // 4
        public float3 position; // 12

        // 20B (3 per cacheline)

        public int CompareTo(Neighbour other)
        {
            return distSq.CompareTo(other.distSq);
        }
    }

#if !ENABLE_KDTREE_VALIDATION_CHECKS
    [BurstCompile]
#endif
    struct PreprocessJob : IJobParallelFor
    {
        internal int Depth;
        internal KDTree This;

        public void Execute(int index)
        {
            uint nodeIndex = (uint)math.pow(2, Depth) + (uint)index;
            This.BuildSubTree(nodeIndex, Depth, true);
        }
    }

#if !ENABLE_KDTREE_VALIDATION_CHECKS
    [BurstCompile]
#endif
    struct BuildSubTreeJob : IJobParallelFor
    {
        internal int Depth;
        internal KDTree This;

        public void Execute(int index)
        {
            uint nodeIndex = (uint)math.pow(2, Depth) + (uint)index;
            This.BuildSubTree(nodeIndex, Depth, false);
        }
    }

    [NativeDisableUnsafePtrRestriction] TreeNode* m_NodesPtr;

    int m_Capacity;
    Allocator m_AllocatorLabel;

    internal int m_NumEntries;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
    internal int m_Length;
    internal int m_MinIndex;
    internal int m_MaxIndex;
    AtomicSafetyHandle m_Safety;
    [NativeSetClassTypeToNullOnSchedule] DisposeSentinel m_DisposeSentinel;
#endif

    [NativeDisableUnsafePtrRestriction] byte* m_EntriesPtr;

    ProfilerMarker m_WalkingTreeMarker;
    ProfilerMarker m_LinearSearchMarker;
    ProfilerMarker m_BestNeighbourSortMarker;

    internal int m_NumWorkers;
    internal int m_MaxDepth;

#if ENABLE_KDTREE_ANALYTICS
    int m_NumNodesVisited;
    int m_NumEntriesCompared;
    int m_NumEntriesFoundOverNeighbourCapacity;
#endif

    public bool IsCreated { get { return m_Capacity > 0; } }

    public static int CalculateNumNodes(int numEntries, out int maxDepth, out int balancedLeafNodes)
    {
        balancedLeafNodes = math.max(1, (numEntries / k_MaxLeafSize));

        // need to ensure we have enough nodes for an eytzinger layout (much more cache performant)
        maxDepth = KDTree.k_MaxUnbalancedDepth + (int)math.ceil(math.log2(balancedLeafNodes));
        return numEntries > k_MaxLeafSize ? (int)math.pow(2f, 1 + maxDepth) : 1;
    }

    public static int CalculateNumWorkers(int numEntries, int maxWorkersAvailable, int maxLeafSize, int minLeavesPerWorker)
    {
        int maxWorkers = (int)math.pow(2f, math.floor(math.log2(maxWorkersAvailable)));

        int minEntriesPerWorker = maxLeafSize * minLeavesPerWorker;

        int numWorkers = 1;
        while ((numWorkers * 2) <= maxWorkers
            && numEntries >= (numWorkers * 2 * minEntriesPerWorker))
        {
            numWorkers *= 2;
        }

        return numWorkers;
    }

    public KDTree(int capacity, Allocator allocator, int MaxWorkerThreads)
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        // Native allocation is only valid for Temp, Job and Persistent
        if (allocator <= Allocator.None)
            throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", "allocator");
        if (capacity < 0)
            throw new ArgumentOutOfRangeException("capacity", "Capacity must be >= 0");
#endif

        int maxDepth, balancedLeafNodes;
        int numNodes = CalculateNumNodes(capacity, out maxDepth, out balancedLeafNodes);
        var workers = CalculateNumWorkers(capacity, JobsUtility.JobWorkerCount, k_MaxLeafSize, k_MinLeavesPerWorker);
        m_NumWorkers = math.min(MaxWorkerThreads, workers);

        m_WalkingTreeMarker = new ProfilerMarker("Walking Tree");
        m_LinearSearchMarker = new ProfilerMarker("Linear Search");
        m_BestNeighbourSortMarker = new ProfilerMarker("Best Neighbour Sort");

#if ENABLE_KDTREE_ANALYTICS
        Debug.Log($"Allocating {numNodes} nodes for {capacity} entries, using {m_NumWorkers} workers with {maxDepth} maxDepth");
        Debug.Log($"Sizeof TreeNode = {sizeof(TreeNode)}, Sizeof Entry = {sizeof(Entry)}, Total tree memory = {(sizeof(TreeNode) * numNodes) / (1024 * 1024):0.0}mb");
#endif

        m_NodesPtr = (TreeNode*)UnsafeUtility.Malloc(sizeof(TreeNode) * numNodes, JobsUtility.CacheLineSize, allocator);

        int entryPadding = m_NumWorkers * JobsUtility.CacheLineSize;
        m_EntriesPtr = (byte*)UnsafeUtility.Malloc((sizeof(Entry) * capacity) + entryPadding, JobsUtility.CacheLineSize, allocator);

        m_Capacity = capacity;
        m_NumEntries = 0;
        m_AllocatorLabel = allocator;

        m_MaxDepth = maxDepth - 2;

#if ENABLE_KDTREE_ANALYTICS
        m_NumNodesVisited = 0;
        m_NumEntriesCompared = 0;
        m_NumEntriesFoundOverNeighbourCapacity = 0;
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        m_Length = m_Capacity;
        m_MinIndex = 0;
        m_MaxIndex = -1;
        DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, allocator);
#endif
    }

    [WriteAccessRequired]
    public void Dispose()
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

        UnsafeUtility.Free(m_EntriesPtr, m_AllocatorLabel);
        m_EntriesPtr = null;

        UnsafeUtility.Free(m_NodesPtr, m_AllocatorLabel);
        m_NodesPtr = null;

        m_Capacity = 0;
    }

    [WriteAccessRequired]
    public void AddEntry(int index, in float3 pos)
    {
        Entry* entryPtr = (Entry*)(m_EntriesPtr + (index * sizeof(Entry)));
        *entryPtr = new Entry { index = index, position = pos };
    }

    public JobHandle BuildTree(int numEntries, JobHandle inputDeps)
    {
        var dep = inputDeps;

        m_NumEntries = numEntries;

        if (m_NumEntries == 0)
        {
            return dep;
        }

        SetNode(k_RootNodeIndex, m_EntriesPtr, m_EntriesPtr + ((m_NumEntries - 1) * sizeof(Entry)));

        if (m_NumEntries > k_MaxLeafSize)
        {
            // calculate how many workers we need based on entries
            var entryNumWorkers = CalculateNumWorkers(m_NumEntries, JobsUtility.JobWorkerCount, k_MaxLeafSize, k_MinLeavesPerWorker);
            entryNumWorkers = math.min(m_NumWorkers, entryNumWorkers);
            var maxDepthOnPreProcess = (int)math.log2(entryNumWorkers);

            // preprocess tree for parallel work
            for (int depth = 0; depth < maxDepthOnPreProcess; depth++)
            {
                int numNodesToProcess = (int)math.pow(2, depth);

                var preProcessJob = new PreprocessJob
                {
                    This = this,
                    Depth = depth,
                };
                dep = preProcessJob.Schedule(numNodesToProcess, 1, dep);
            }

            //build tree on workers
            var buildSubTreeJob = new BuildSubTreeJob
            {
                This = this,
                Depth = maxDepthOnPreProcess,
            };
            dep = buildSubTreeJob.Schedule(entryNumWorkers, 1, dep);
        }

        return dep;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static unsafe Bounds FindNodeBounds(TreeNode* nodePtr, byte* beginPtr, byte* endPtr, out float3 mean)
    {
        var bounds = new Bounds
        {
            min = new float3(float.MaxValue),
            max = new float3(float.MinValue)
        };
        mean = new float3(0f);

        int count = 0;
        for (byte* entryPtr = beginPtr; entryPtr <= endPtr; entryPtr += sizeof(Entry))
        {
            var ptr = (Entry*)entryPtr;
            ExpandBounds(ref bounds, ptr->position);
            mean += ptr->position;
            count++;
        }

        mean /= count;

        return bounds;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static SphereBounds GetSphereBoundsFromBounds(in Bounds bounds)
    {
        var centre = new float3((bounds.max.x + bounds.min.x) / 2f, (bounds.max.y + bounds.min.y) / 2f, (bounds.max.z + bounds.min.z) / 2f);
        float radius = math.distance(bounds.max, centre);

        return new SphereBounds
        {
            centre = centre,
            radius = radius
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int CalculateSplitDimension(in Bounds bounds, in float3 meanPos, out float splitValue)
    {
        float lengthX = bounds.max.x - bounds.min.x;
        float lengthY = bounds.max.y - bounds.min.y;
        float lengthZ = bounds.max.z - bounds.min.z;

        if (lengthX >= lengthY && lengthX >= lengthZ)
        {
            splitValue = meanPos.x;
            return 0;
        }
        else if (lengthY >= lengthX && lengthY >= lengthZ)
        {
            splitValue = meanPos.y;
            return 1;
        }
        else
        {
            splitValue = meanPos.z;
            return 2;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float GetDimensionComponent(int dimension, in float3 pos)
    {
        return dimension == 0 ? pos.x : dimension == 1 ? pos.y : pos.z;
    }

    void BuildSubTree(uint nodeIndex, int depth, bool preProcess = false)
    {
        TreeNode* nodePtr = m_NodesPtr + nodeIndex;

        uint count = nodePtr->Count;
        byte* beginPtr = nodePtr->beginPtr;
        byte* endPtr = nodePtr->beginPtr + (count - 1) * sizeof(Entry);

        //if (preProcess)
        //    Debug.Log($"Pre-processing node {nodeIndex}, depth {depth}, count {count}");
        //else
        //    Debug.Log($"Building subtree node {nodeIndex}, depth {depth}, count {count}");

        if (count == 0)
        {
            // preprocessing resulted in an unbalanced tree
            // so if this is a leaf node, we should drop out now
            nodePtr->count |= k_IsLeafNodeBitFlag;

            if (preProcess)
            {
                uint leftNode = 2 * nodeIndex;
                uint rightNode = leftNode + 1;

                // fill in left/right node details
                SetEmptyLeafNode(leftNode);
                SetEmptyLeafNode(rightNode);
            }
            return;
        }

        float3 mean;
        var bounds = FindNodeBounds(nodePtr, beginPtr, endPtr, out mean);
        nodePtr->bounds = GetSphereBoundsFromBounds(bounds);

        if (depth < m_MaxDepth
            && nodePtr->bounds.radius >= k_ZeroRadiusEpsilon
            && count > k_MaxLeafSize)
        {
            // as we are preprocessing, we know we are not a leaf node, so we can split
            depth++;

            // split into left/right
            float splitValue;
            int splitDimension = CalculateSplitDimension(bounds, mean, out splitValue);

            byte* leftPtr = beginPtr;
            byte* rightPtr = endPtr;

            int rightPtrOffset = preProcess ? (int)(m_NumWorkers / math.pow(2f, depth)) * JobsUtility.CacheLineSize : 0;
            byte* rightDestPtr = rightPtr + rightPtrOffset;

            while (leftPtr < rightPtr)
            {
                // while left positions are on the left, skip to next
                while (leftPtr < rightPtr && GetDimensionComponent(splitDimension, ((Entry*)leftPtr)->position) < splitValue)
                {
                    leftPtr += sizeof(Entry);
                }

                // while right positions are on the right, skip to next
                while (rightPtr > leftPtr && GetDimensionComponent(splitDimension, ((Entry*)rightPtr)->position) >= splitValue)
                {
                    // copy item to dest ptr
                    // (to ensure the split is cacheline aligned)
                    *(Entry*)rightDestPtr = *(Entry*)rightPtr;

                    rightPtr -= sizeof(Entry);
                    rightDestPtr -= sizeof(Entry);
                }

                // if entries are on the wrong side, swap them over
                if (leftPtr < rightPtr)
                {
                    Entry temp = *(Entry*)rightPtr;
                    *(Entry*)rightPtr = *(Entry*)leftPtr;
                    *(Entry*)rightDestPtr = *(Entry*)leftPtr;
                    *(Entry*)leftPtr = temp;

                    leftPtr += sizeof(Entry);
                    rightPtr -= sizeof(Entry);
                    rightDestPtr -= sizeof(Entry);
                }
            }

            *(Entry*)rightDestPtr = *(Entry*)rightPtr;

            // find pivot
            while (leftPtr > beginPtr && GetDimensionComponent(splitDimension, ((Entry*)leftPtr)->position) >= splitValue)
            {
                leftPtr -= sizeof(Entry);
            }

            uint leftNode = 2 * nodeIndex;
            uint rightNode = leftNode + 1;

            SetNode(leftNode, beginPtr, leftPtr);
            if (!preProcess)
                BuildSubTree(leftNode, depth);

            SetNode(rightNode, leftPtr + sizeof(Entry) + rightPtrOffset, endPtr + rightPtrOffset);
            if (!preProcess)
                BuildSubTree(rightNode, depth);

#if ENABLE_UNITY_COLLECTIONS_CHECKS && ENABLE_KDTREE_VALIDATION_CHECKS
            CheckNode(leftNode, depth);
            CheckNode(rightNode, depth);

            CheckChildNodes(leftNode, rightNode, count);
#endif
        }
        else
        {
            // this is a leaf node
            nodePtr->count |= k_IsLeafNodeBitFlag;

            if (preProcess)
            {
                uint leftNode = 2 * nodeIndex;
                uint rightNode = leftNode + 1;

                // fill in left/right node details
                SetEmptyLeafNode(leftNode);
                SetEmptyLeafNode(rightNode);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void SetNode(uint nodeIndex, byte* beginPtr, byte* endPtr)
    {
        TreeNode* nodePtr = m_NodesPtr + nodeIndex;
        *nodePtr = new TreeNode
        {
            beginPtr = beginPtr,
            count = (uint)((endPtr - beginPtr) / sizeof(Entry)) + 1
        };

        //if (nodePtr->count == 0 || nodePtr->count > 10000)
        //    throw new InvalidOperationException($"SetNode on {nodeIndex} ({nodePtr->nodeIndex}) with invalid count {nodePtr->count}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void SetEmptyLeafNode(uint nodeIndex)
    {
        TreeNode* nodePtr = m_NodesPtr + nodeIndex;
        *nodePtr = new TreeNode
        {
            count = k_IsLeafNodeBitFlag,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void ExpandBounds(ref Bounds bounds, in float3 pos)
    {
        bounds.min.x = math.min(bounds.min.x, pos.x);
        bounds.max.x = math.max(bounds.max.x, pos.x);
        bounds.min.y = math.min(bounds.min.y, pos.y);
        bounds.max.y = math.max(bounds.max.y, pos.y);
        bounds.min.z = math.min(bounds.min.z, pos.z);
        bounds.max.z = math.max(bounds.max.z, pos.z);
    }

    public void BeginAnalyticsCapture()
    {
#if ENABLE_KDTREE_ANALYTICS
        m_NumNodesVisited = 0;
        m_NumEntriesCompared = 0;
        m_NumEntriesFoundOverNeighbourCapacity = 0;
#endif
    }

    public void EndAnalyticsCapture()
    {
#if ENABLE_KDTREE_ANALYTICS
        var nodeSizeBuckets = new NativeArray<int>(k_MaxLeafSize + 2, Allocator.Temp);
        PopulateNodeSizeBucketsRecursive(k_RootNodeIndex, ref nodeSizeBuckets);

        Debug.Log("\nBuilding:");
        for (int i = 0; i < k_MaxLeafSize + 2; i++)
        {
            Debug.Log($"Leaves with {i} entries = {nodeSizeBuckets[i]}");
        }

        Debug.Log("\nQuerying:");
        Debug.Log($"NumNodesVisited = {m_NumNodesVisited}");
        Debug.Log($"NumEntriesCompared = {m_NumEntriesCompared}");
        Debug.Log($"NumEntriesFoundOverNeighbourCapacity = {m_NumEntriesFoundOverNeighbourCapacity}");
#endif
    }

#if ENABLE_KDTREE_ANALYTICS
    void PopulateNodeSizeBucketsRecursive(uint nodeIndex, ref NativeArray<int> nodeSizeBuckets)
    {
        TreeNode* nodePtr = m_NodesPtr + nodeIndex;
        if ((nodePtr->count & k_IsLeafNodeBitFlag) > 0)
        {
            // leaf
            uint count = nodePtr->Count;
            int bucketCount = math.min((int)count, k_MaxLeafSize + 1);
            nodeSizeBuckets[bucketCount]++;
        }
        else
        {
            uint leftNodeIndex = 2 * nodeIndex;
            uint rightNodeIndex = leftNodeIndex + 1;
            PopulateNodeSizeBucketsRecursive(leftNodeIndex, ref nodeSizeBuckets);
            PopulateNodeSizeBucketsRecursive(rightNodeIndex, ref nodeSizeBuckets);
        }
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float GetDistanceToBounds(in float3 position, in SphereBounds bounds)
    {
        return math.distancesq(position, bounds.centre);
    }

    static bool DoesSearchInteresectBounds(float distanceSq, float radius, in SphereBounds bounds)
    {
        float totalRadius = radius + bounds.radius;
        totalRadius *= totalRadius;

        return distanceSq <= totalRadius;
    }

    public int GetEntriesInRangeWithHeap(int queryingIndex, in float3 position, float range, ref NativePriorityHeap<Neighbour> neighbours)
    {
        if (m_NumEntries == 0)
            return 0;

        QueryTreeRecursive(queryingIndex, position, ref range, k_RootNodeIndex, ref neighbours, 0);

        return neighbours.Count;
    }

    public int GetEntriesInRange(int queryingIndex, in float3 position, float range, ref NativeArray<Neighbour> neighbours)
    {
        if (m_NumEntries == 0)
            return 0;

        var neighboursAsPriorityHeap = NativePriorityHeap<Neighbour>.FromArray(neighbours, 0, NativePriorityHeap.Comparison.Max);

        QueryTreeRecursive(queryingIndex, position, ref range, k_RootNodeIndex, ref neighboursAsPriorityHeap, 0);

        neighbours = neighboursAsPriorityHeap.AsArray();
        return neighboursAsPriorityHeap.Count;
    }

    void QueryTreeRecursive(int queryingIndex, in float3 position, ref float range, uint nodeIndex, ref NativePriorityHeap<Neighbour> neighboursAsPriorityHeap, int numNeighbours)
    {
        TreeNode* nodePtr = m_NodesPtr + nodeIndex;

#if ENABLE_KDTREE_ANALYTICS
        m_NumNodesVisited++;
#endif

        // is this a leaf node
        // do dist check
        if (nodePtr->IsLeaf)
        {
            SearchEntriesInRangeWithHeap(nodePtr, queryingIndex, position, ref range, ref neighboursAsPriorityHeap);
        }
        else
        {
            uint leftNodeIndex = 2 * nodeIndex;
            TreeNode* leftNodePtr = m_NodesPtr + leftNodeIndex;
            float leftRadius = leftNodePtr->bounds.radius;

            uint rightNodeIndex = leftNodeIndex + 1;
            TreeNode* rightNodePtr = m_NodesPtr + rightNodeIndex;
            float rightRadius = rightNodePtr->bounds.radius;

            float distSqLeft = math.distancesq(leftNodePtr->bounds.centre, position);
            float distSqRight = math.distancesq(rightNodePtr->bounds.centre, position);

            // the min collide distance is the query radius (h) + the node bounding radius (r)
            // if h + r > distance then we have an entry that could potentially be inside this bound
            float leftCollideDist = leftRadius + range;
            float rightCollideDist = rightRadius + range;

            if (distSqLeft <= distSqRight)
            {
                if ((leftCollideDist * leftCollideDist) >= distSqLeft)
                    QueryTreeRecursive(queryingIndex, position, ref range, leftNodeIndex, ref neighboursAsPriorityHeap, numNeighbours);

                if ((rightCollideDist * rightCollideDist) >= distSqRight)
                    QueryTreeRecursive(queryingIndex, position, ref range, rightNodeIndex, ref neighboursAsPriorityHeap, numNeighbours);
            }
            else
            {
                if ((rightCollideDist * rightCollideDist) >= distSqRight)
                    QueryTreeRecursive(queryingIndex, position, ref range, rightNodeIndex, ref neighboursAsPriorityHeap, numNeighbours);

                if ((leftCollideDist * leftCollideDist) >= distSqLeft)
                    QueryTreeRecursive(queryingIndex, position, ref range, leftNodeIndex, ref neighboursAsPriorityHeap, numNeighbours);
            }
        }
    }

    public void SearchEntriesInRangeWithHeap(TreeNode* nodePtr, int queryingIndex, in float3 position, ref float range, ref NativePriorityHeap<Neighbour> neighbours)
    {
        uint count = nodePtr->Count;
        byte* beginPtr = nodePtr->beginPtr;
        byte* endPtr = nodePtr->beginPtr + (count - 1) * sizeof(Entry);

        float rangeSq = range * range;
        for (byte* entryPtr = beginPtr; entryPtr <= endPtr; entryPtr += sizeof(Entry))
        {
            var ptr = (Entry*)entryPtr;

            if (queryingIndex != ptr->index)
            {
#if ENABLE_KDTREE_ANALYTICS
                m_NumEntriesCompared++;
#endif

                float distSq = math.distancesq(position, ptr->position);

                if (distSq <= rangeSq)
                {
                    if (neighbours.Count < neighbours.Capacity)
                    {
                        neighbours.Push(new Neighbour { index = ptr->index, distSq = distSq, position = ptr->position });
                    }
                    else
                    {
#if ENABLE_KDTREE_ANALYTICS
                        m_NumEntriesFoundOverNeighbourCapacity++;
#endif
                        // pop furthest off heap
                        neighbours.Pop();
                        neighbours.Push(new Neighbour { index = ptr->index, distSq = distSq, position = ptr->position });
                    }

                    if (neighbours.Count >= neighbours.Capacity)
                    {
                        rangeSq = neighbours.Peek().distSq;
                        range = math.sqrt(rangeSq);
                    }
                }
            }
        }
    }

#if ENABLE_UNITY_COLLECTIONS_CHECKS && ENABLE_KDTREE_VALIDATION_CHECKS
    internal void CheckNode(uint nodeIndex, int depth)
    {
        TreeNode* nodePtr = m_NodesPtr + nodeIndex;

        // check that each entry matches up
        uint count = nodePtr->Count;

        if (count > m_Capacity || count == 0)
            throw new InvalidOperationException($"Node at index {nodeIndex} has corrupt count {count} at depth {depth}");

        byte* beginPtr = nodePtr->beginPtr;
        if (beginPtr == null)
            throw new InvalidOperationException($"Node at index {nodeIndex} has null begin ptr (count {count}, depth {depth}");

        byte* endPtr = nodePtr->beginPtr + (count - 1) * sizeof(Entry);

        int i = 0;
        for (byte* entryPtr = beginPtr; entryPtr <= endPtr; entryPtr += sizeof(Entry), i++)
        {
            var entry = *(Entry*)entryPtr;

            int j = 0;
            for (byte* innerEntryPtr = beginPtr; innerEntryPtr <= endPtr; innerEntryPtr += sizeof(Entry), j++)
            {
                if (i != j)
                {
                    var compare = *(Entry*)innerEntryPtr;
                    if (compare.index == entry.index)
                        throw new InvalidOperationException($"Node at index {nodeIndex} has duplicated entities at {i} and {j} (count {count})");
                }
            }
        }
    }

    internal void CheckChildNodes(uint leftNode, uint rightNode, uint parentCount)
    {
        var leftNodePtr = m_NodesPtr + leftNode;
        var rightNodePtr = m_NodesPtr + rightNode;

        uint totalChildCount = (leftNodePtr->Count) + (rightNodePtr->Count);
        if (totalChildCount != parentCount)
            throw new InvalidOperationException($"Total left/right count {totalChildCount} does not match parent count {parentCount}");
    }
#endif

}