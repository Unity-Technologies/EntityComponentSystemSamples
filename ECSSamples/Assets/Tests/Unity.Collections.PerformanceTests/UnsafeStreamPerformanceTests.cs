using NUnit.Framework;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
#if !UNITY_DOTSRUNTIME
using Unity.PerformanceTesting;

internal class UnsafeStreamPerformanceTests
{
    [BurstCompile]
    private class Pointers
	{
        [BurstCompile(CompileSynchronously = true)]
        public static void StreamWrite(ref UnsafeStream stream, int numElements)
        {
            var writer = stream.AsWriter();

            for (int i = 0; i < numElements; ++i)
            {
                writer.Write(i);
            }
        }

        public delegate void StreamWriteDelegate(ref UnsafeStream stream, int numElements);
    }

    [Test, Performance]
    [Category("Performance")]
    public void UnsafeStream_Performance_Write()
    {
        const int numElements = 16 << 10;

        var stream = new UnsafeStream(1, Allocator.Persistent);

        var writer = stream.AsWriter();

        Measure.Method(() =>
        {
            for (int i = 0; i < numElements; ++i)
            {
                writer.Write(i);
            }
        })
            .WarmupCount(100)
            .MeasurementCount(1000)
            .Run();

        stream.Dispose();
    }

    [Test, Performance]
    [Category("Performance")]
    public void UnsafeStream_Performance_Write_Burst()
    {
        const int numElements = 16 << 10;

        var stream = new UnsafeStream(1, Allocator.Persistent);

        var funcPtr = BurstCompiler.CompileFunctionPointer<Pointers.StreamWriteDelegate>(Pointers.StreamWrite);

        Measure.Method(() =>
        {
            funcPtr.Invoke(ref stream, numElements);
        })
            .WarmupCount(100)
            .MeasurementCount(1000)
            .Run();

        stream.Dispose();
    }
}

#endif
