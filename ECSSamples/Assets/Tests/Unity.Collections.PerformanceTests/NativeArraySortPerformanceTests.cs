using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
#if !UNITY_DOTSRUNTIME
using Unity.PerformanceTesting;

internal class NativeSlicePerformanceTests
{
    [Test, Performance]
    [Category("Performance")]
    public void NativeSlice_Performance_CopyTo()
    {
        const int numElements = 16 << 10;

        NativeArray<int> array = new NativeArray<int>(numElements, Allocator.Persistent);
        var slice = new NativeSlice<int>(array, 0, numElements);

        var copyToArray = new int[numElements];

        Measure.Method(() =>
        {
            slice.CopyTo(copyToArray);
        })
            .WarmupCount(100)
            .MeasurementCount(1000)
            .Run();

        array.Dispose();
    }

    [Test, Performance]
    [Category("Performance")]
    public void NativeSlice_Performance_CopyFrom()
    {
        const int numElements = 16 << 10;

        NativeArray<int> array = new NativeArray<int>(numElements, Allocator.Persistent);
        var slice = new NativeSlice<int>(array, 0, numElements);

        var copyToArray = new int[numElements];

        Measure.Method(() =>
        {
            slice.CopyFrom(copyToArray);
        })
            .WarmupCount(100)
            .MeasurementCount(1000)
            .Run();

        array.Dispose();
    }
}

#endif
