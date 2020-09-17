using System;
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
        internal struct JobNativeMultiHashMapMergedSharedKeyIndicesProducer<TJob>
            where TJob : struct, IJobNativeMultiHashMapMergedSharedKeyIndices
        {
            [ReadOnly] public NativeMultiHashMap<int, int> HashMap;
            internal TJob JobData;

            private static IntPtr s_JobReflectionData;

            internal static IntPtr Initialize()
            {
                if (s_JobReflectionData == IntPtr.Zero)
                {
#if UNITY_2020_2_OR_NEWER
                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(JobNativeMultiHashMapMergedSharedKeyIndicesProducer<TJob>), typeof(TJob), (ExecuteJobFunction)Execute);
#else
                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(JobNativeMultiHashMapMergedSharedKeyIndicesProducer<TJob>), typeof(TJob), JobType.ParallelFor, (ExecuteJobFunction)Execute);
#endif
                }

                return s_JobReflectionData;
            }

            delegate void ExecuteJobFunction(ref JobNativeMultiHashMapMergedSharedKeyIndicesProducer<TJob> jobProducer, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public static unsafe void Execute(ref JobNativeMultiHashMapMergedSharedKeyIndicesProducer<TJob> jobProducer, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    int begin;
                    int end;

                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        return;
                    }

                    var bucketData = jobProducer.HashMap.GetUnsafeBucketData();
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
                            jobProducer.HashMap.TryGetFirstValue(key, out firstValue, out it);

                            // [macton] Didn't expect a usecase for this with multiple same values
                            // (since it's intended use was for unique indices.)
                            // https://forum.unity.com/threads/ijobnativemultihashmapmergedsharedkeyindices-unexpected-behavior.569107/#post-3788170
                            if (entryIndex == it.GetEntryIndex())
                            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                                JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobProducer), value, 1);
#endif
                                jobProducer.JobData.ExecuteFirst(value);
                            }
                            else
                            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                                var startIndex = math.min(firstValue, value);
                                var lastIndex = math.max(firstValue, value);
                                var rangeLength = (lastIndex - startIndex) + 1;

                                JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobProducer), startIndex, rangeLength);
#endif
                                jobProducer.JobData.ExecuteNext(firstValue, value);
                            }

                            entryIndex = nextPtrs[entryIndex];
                        }
                    }
                }
            }
        }

        public static unsafe JobHandle Schedule<TJob>(this TJob jobData, NativeMultiHashMap<int, int> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = new JobHandle())
            where TJob : struct, IJobNativeMultiHashMapMergedSharedKeyIndices
        {
            var jobProducer = new JobNativeMultiHashMapMergedSharedKeyIndicesProducer<TJob>
            {
                HashMap = hashMap,
                JobData = jobData
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobProducer)
                , JobNativeMultiHashMapMergedSharedKeyIndicesProducer<TJob>.Initialize()
                , dependsOn
#if UNITY_2020_2_OR_NEWER
                , ScheduleMode.Parallel
#else
                , ScheduleMode.Batched
#endif
            );

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, hashMap.GetUnsafeBucketData().bucketCapacityMask + 1, minIndicesPerJobCount);
        }
    }
}
