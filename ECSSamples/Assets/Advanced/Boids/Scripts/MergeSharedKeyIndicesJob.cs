using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Samples.Boids
{
    // IJobNativeMultiHashMapMergedSharedKeyIndices: custom job type, following its own defined custom safety rules:
    // A) because we know how hashmap safety works, B) we can iterate safely in parallel
    // Notable Features:
    // 1) The hash map must be a NativeMultiHashMap<int,int>, where the key is a hash of some data, and the index is
    // a unique index (generally to the relevant data in some other collection).
    // 2) Each bucket is processed concurrently with other buckets.
    // 3) All key/value pairs in each bucket are processed individually (in sequential order) by a single thread.
    [JobProducerType(typeof(JobNativeMultiHashMapUniqueHashExtensions.JobNativeMultiHashMapMergedSharedKeyIndicesProducer<>))]
    public interface IJobNativeMultiHashMapMergedSharedKeyIndices
    {
        // The first time each key (=hash) is encountered, ExecuteFirst() is invoked with corresponding value (=index).
        void ExecuteFirst(int index);

        // For each subsequent instance of the same key in the bucket, ExecuteNext() is invoked with the corresponding
        // value (=index) for that key, as well as the value passed to ExecuteFirst() the first time this key
        // was encountered (=firstIndex).
        void ExecuteNext(int firstIndex, int index);
    }

    public static class JobNativeMultiHashMapUniqueHashExtensions
    {
        internal struct JobWrapper<T> where T : struct
        {
            [ReadOnly] public NativeMultiHashMap<int, int> HashMap;
            public T JobData;
        }

        /// <summary>
        /// Gathers and caches reflection data for the internal job system's managed bindings. Unity is responsible for calling this method - don't call it yourself.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <remarks>
        /// When the Jobs package is included in the project, Unity generates code to call EarlyJobInit at startup. This allows Burst compiled code to schedule jobs because the reflection part of initialization, which is not compatible with burst compiler constraints, has already happened in EarlyJobInit.
        ///
        /// __Note__: While the Jobs package code generator handles this automatically for all closed job types, you must register those with generic arguments (like IJobChunk&amp;lt;MyJobType&amp;lt;T&amp;gt;&amp;gt;) manually for each specialization with [[Unity.Jobs.RegisterGenericJobTypeAttribute]].
        /// </remarks>
        public static void EarlyJobInit<T>()
            where T : struct, IJobNativeMultiHashMapMergedSharedKeyIndices
        {
            JobNativeMultiHashMapMergedSharedKeyIndicesProducer<T>.Initialize();
        }

        public static unsafe JobHandle Schedule<T>(this T jobData, NativeMultiHashMap<int, int> hashMap,
                int minIndicesPerJobCount, JobHandle dependsOn = default)
            where T : struct, IJobNativeMultiHashMapMergedSharedKeyIndices
        {
            var jobWrapper = new JobWrapper<T>
            {
                HashMap = hashMap,
                JobData = jobData,
            };
            JobNativeMultiHashMapMergedSharedKeyIndicesProducer<T>.Initialize();
            var reflectionData = JobNativeMultiHashMapMergedSharedKeyIndicesProducer<T>.reflectionData.Data;
            CollectionHelper.CheckReflectionDataCorrect<T>(reflectionData);

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobWrapper),
                reflectionData,
                dependsOn,
                ScheduleMode.Parallel);

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, hashMap.GetUnsafeBucketData().bucketCapacityMask + 1, minIndicesPerJobCount);
        }


        [BurstCompile]
        internal struct JobNativeMultiHashMapMergedSharedKeyIndicesProducer<T>
            where T : struct, IJobNativeMultiHashMapMergedSharedKeyIndices
        {
            internal static readonly SharedStatic<IntPtr> reflectionData = SharedStatic<IntPtr>.GetOrCreate<JobNativeMultiHashMapMergedSharedKeyIndicesProducer<T>>();

            [BurstDiscard]
            internal static void Initialize()
            {
                if (reflectionData.Data == IntPtr.Zero)
                    reflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(JobWrapper<T>), typeof(T), (ExecuteJobFunction)Execute);
            }

            delegate void ExecuteJobFunction(ref JobWrapper<T> jobWrapper, IntPtr additionalPtr, IntPtr bufferRangePatchData,
                ref JobRanges ranges, int jobIndex);

            [BurstCompile]
            public static unsafe void Execute(ref JobWrapper<T> jobWrapper, IntPtr additionalPtr, IntPtr bufferRangePatchData,
                ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    int begin;
                    int end;

                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        return;
                    }

                    var bucketData = jobWrapper.HashMap.GetUnsafeBucketData();
                    var buckets = (int*)bucketData.buckets;
                    var nextPtrs = (int*)bucketData.next;
                    var keys = bucketData.keys;
                    var values = bucketData.values;

                    for (int i = begin; i < end; i++)
                    {
                        int entryIndex = buckets[i];

                        while (entryIndex != -1)
                        {
                            var key = UnsafeUtility.ReadArrayElement<int>(keys, entryIndex);
                            var value = UnsafeUtility.ReadArrayElement<int>(values, entryIndex);
                            int firstValue;

                            NativeMultiHashMapIterator<int> it;
                            jobWrapper.HashMap.TryGetFirstValue(key, out firstValue, out it);

                            // [macton] Didn't expect a usecase for this with multiple same values
                            // (since it's intended use was for unique indices.)
                            // https://forum.unity.com/threads/ijobnativemultihashmapmergedsharedkeyindices-unexpected-behavior.569107/#post-3788170
                            if (entryIndex == it.GetEntryIndex())
                            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                                JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobWrapper), value, 1);
#endif
                                jobWrapper.JobData.ExecuteFirst(value);
                            }
                            else
                            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                                var startIndex = math.min(firstValue, value);
                                var lastIndex = math.max(firstValue, value);
                                var rangeLength = (lastIndex - startIndex) + 1;

                                JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobWrapper), startIndex, rangeLength);
#endif
                                jobWrapper.JobData.ExecuteNext(firstValue, value);
                            }

                            entryIndex = nextPtrs[entryIndex];
                        }
                    }
                }
            }
        }
    }
}
