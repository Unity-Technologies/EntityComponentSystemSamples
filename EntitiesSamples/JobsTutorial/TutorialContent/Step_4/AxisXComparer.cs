using System.Collections.Generic;
using Unity.Mathematics;

public struct AxisXComparer : IComparer<float3>
{
    public int Compare(float3 a, float3 b)
    {
        return a.x.CompareTo(b.x);
    }
}