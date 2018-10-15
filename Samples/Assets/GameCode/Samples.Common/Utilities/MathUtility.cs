using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Samples.Common
{
    public struct GeneratePoints
    {
        static public void RandomPointsInSphere(float3 center, float radius, ref NativeArray<float3> points)
        {
            var radiusSquared = radius * radius;
            var pointsFound = 0;
            var count = points.Length;
            while (pointsFound < count)
            {
                var p = new float3
                {
                    x = UnityEngine.Random.Range(-radius, radius),
                    y = UnityEngine.Random.Range(-radius, radius),
                    z = UnityEngine.Random.Range(-radius, radius)
                };
                if (math.lengthsq(p) < radiusSquared)
                {
                    points[pointsFound] = center + p;
                    pointsFound++;
                }
            }
        }

        static public void RandomPointsOnCircle(float3 center, float radius, ref NativeArray<float3> points)
        {
            var count = points.Length;
            for (int i = 0; i < count; i++)
            {
                float angle = UnityEngine.Random.Range(0.0f, Mathf.PI * 2.0f);
                points[i] = center + new float3
                {
                    x = math.sin(angle) * radius,
                    y = 0,
                    z = math.cos(angle) * radius
                };
            }
        }
    }
}
