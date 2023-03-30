using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

// We'll use Unity.Mathematics.float3 instead of Vector3,
// and we'll use Unity.Mathematics.math.distancesq instead of Vector3.sqrMagnitude.
using Unity.Mathematics;

namespace Tutorials.Jobs.Step2
{
    // Include the BurstCompile attribute to Burst compile the job.
    [BurstCompile]
    public struct FindNearestJob : IJob
    {
        // All of the data which a job will access should 
        // be included in its fields. In this case, the job needs
        // three arrays of float3.

        // Array and collection fields that are only read in
        // the job should be marked with the ReadOnly attribute.
        // Although not strictly necessary in this case, marking data  
        // as ReadOnly may allow the job scheduler to safely run 
        // more jobs concurrently with each other.
        // (See the "Intro to jobs" for more detail.)

        [ReadOnly] public NativeArray<float3> TargetPositions;
        [ReadOnly] public NativeArray<float3> SeekerPositions;

        // For SeekerPositions[i], we will assign the nearest 
        // target position to NearestTargetPositions[i].
        public NativeArray<float3> NearestTargetPositions;

        // 'Execute' is the only method of the IJob interface.
        // When a worker thread executes the job, it calls this method.
        public void Execute()
        {
            // Compute the square distance from each seeker to every target.
            for (int i = 0; i < SeekerPositions.Length; i++)
            {
                float3 seekerPos = SeekerPositions[i];
                float nearestDistSq = float.MaxValue;
                for (int j = 0; j < TargetPositions.Length; j++)
                {
                    float3 targetPos = TargetPositions[j];
                    float distSq = math.distancesq(seekerPos, targetPos);
                    if (distSq < nearestDistSq)
                    {
                        nearestDistSq = distSq;
                        NearestTargetPositions[i] = targetPos;
                    }
                }
            }
        }
    }
}