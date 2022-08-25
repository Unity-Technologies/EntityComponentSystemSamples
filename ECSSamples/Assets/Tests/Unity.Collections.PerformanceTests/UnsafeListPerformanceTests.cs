using NUnit.Framework;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
#if !UNITY_DOTSRUNTIME
using Unity.PerformanceTesting;

internal class UnsafeListPerformanceTests
{
    [Test, Performance]
    [Category("Performance")]
    public void UnsafeListT_Performance_Add()
    {
        const int numElements = 16 << 10;

        var list = new UnsafeList<int>(1, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        Measure.Method(() =>
        {
            list.Length = 1;
            list.TrimExcess();

            for (int i = 0; i < numElements; ++i)
            {
                list.Add(i);
            }
        })
            .WarmupCount(100)
            .MeasurementCount(1000)
            .Run();

        list.Dispose();
    }

    private struct TestStruct
    {
        public int x;
        public short y;
        public bool z;
    }

    [Test, Performance]
    [Category("Performance")]
    public unsafe void UnsafeUtility_ReadArrayElement_Performance()
    {
        const int numElements = 16 << 10;

        var list = new UnsafeList<TestStruct>(numElements, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        for (int i = 0; i < numElements; ++i)
        {
            list.Add(new TestStruct { x = i, y = (short)(i+1), z = true });
        }

        Measure.Method(() =>
        {
            for(int i = 0; i < numElements; ++i)
            {
                UnsafeUtility.ReadArrayElement<TestStruct>(list.Ptr, i);
            }
        })
            .WarmupCount(100)
            .MeasurementCount(1000)
            .Run();

        list.Dispose();
    }

    [Test, Performance]
    [Category("Performance")]
    public unsafe void UnsafeUtility_ReadArrayElementBoundsChecked_Performance()
    {
        const int numElements = 16 << 10;

        var list = new UnsafeList<TestStruct>(numElements, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        for (int i = 0; i < numElements; ++i)
        {
            list.Add(new TestStruct { x = i, y = (short)(i + 1), z = true });
        }

        Measure.Method(() =>
        {
            for (int i = 0; i < numElements; ++i)
            {
                UnsafeUtilityExtensions.ReadArrayElementBoundsChecked<TestStruct>(list.Ptr, i, numElements);
            }
        })
            .WarmupCount(100)
            .MeasurementCount(1000)
            .Run();

        list.Dispose();
    }

    [Test, Performance]
    [Category("Performance")]
    public unsafe void UnsafeUtility_WriteArrayElement_Performance()
    {
        const int numElements = 16 << 10;

        var list = new UnsafeList<TestStruct>(numElements, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        var test = new TestStruct { x = 0, y = 1, z = true };
        Measure.Method(() =>
        {
            for (int i = 0; i < numElements; ++i)
            {
                UnsafeUtility.WriteArrayElement(list.Ptr, i, test);
            }
        })
            .WarmupCount(100)
            .MeasurementCount(1000)
            .Run();

        list.Dispose();
    }

    [Test, Performance]
    [Category("Performance")]
    public unsafe void UnsafeUtility_WriteArrayElementBoundsChecked_Performance()
    {
        const int numElements = 16 << 10;

        var list = new UnsafeList<TestStruct>(numElements, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        var test = new TestStruct { x = 0, y = 1, z = true };
        Measure.Method(() =>
        {
            for (int i = 0; i < numElements; ++i)
            {
                UnsafeUtilityExtensions.WriteArrayElementBoundsChecked(list.Ptr, i, test, numElements);
            }
        })
            .WarmupCount(100)
            .MeasurementCount(1000)
            .Run();

        list.Dispose();
    }
}

#endif
