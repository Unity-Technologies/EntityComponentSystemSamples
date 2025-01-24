using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Tutorials.Jobs.Step3
{
    [BurstCompile]
    public struct FindNearestJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> TargetPositions;
        [ReadOnly] public NativeArray<float3> SeekerPositions;

        public NativeArray<float3> NearestTargetPositions;

        public void Execute(int index)
        {
            float3 seekerPos = SeekerPositions[index];
            float nearestDistSq = float.MaxValue;
            for (int i = 0; i < TargetPositions.Length; i++)
            {
                float3 targetPos = TargetPositions[i];
                float distSq = math.distancesq(seekerPos, targetPos);
                if (distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    NearestTargetPositions[index] = targetPos;
                }
            }
        }
    }
}