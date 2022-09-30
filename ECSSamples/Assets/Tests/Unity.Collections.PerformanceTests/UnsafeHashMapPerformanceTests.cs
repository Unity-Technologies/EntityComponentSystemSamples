using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
#if !UNITY_DOTSRUNTIME
using Random = UnityEngine.Random;
using Unity.PerformanceTesting;

internal class UnsafeHashMapPerformanceTests
{
    [Test, Performance]
    [Category("Performance")]
    public void UnsafeHashMap_Performance_IsEmpty([Values(10, 100, 1000, 10000, 100000, 1000000, 2500000, 10000000)] int capacity)
    {
        using (var container = new UnsafeHashMap<int, int>(capacity, Allocator.Persistent))
        {
            container.Add(1, 1);

            Measure.Method(() =>
            {
                _ = container.IsEmpty;
            })
                .WarmupCount(10)
                .MeasurementCount(10)
                .Run();
        }
    }

    [Test, Performance]
    [Category("Performance")]
    public void UnsafeHashMap_Performance_Count([Values(10, 100, 1000, 10000, 100000, 1000000, 2500000, 10000000)] int capacity)
    {
        using (var container = new UnsafeHashMap<int, int>(capacity, Allocator.Persistent))
        {
            container.Add(1, 1);

            Measure.Method(() =>
            {
                _ = container.Count;
            })
                .WarmupCount(10)
                .MeasurementCount(10)
                .Run();
        }
    }

    [Test, Performance]
    [Category("Performance")]
    public void UnsafeHashMap_Performance_RepeatInsert([Values(10, 100, 1000, 10000, 100000, 1000000, 2500000)] int insertions)
    {
        using (var container = new UnsafeHashMap<int, int>(insertions, Allocator.Persistent))
        {
            Random.InitState(0);
            Measure.Method(() =>
                {
                    for (int i = 0; i < insertions; i++)
                    {
                        int randKey = Random.Range(0, insertions);
                        container.TryAdd(randKey, randKey);
                    }

                })
                .WarmupCount(10)
                .MeasurementCount(10)
                .Run();
        }
    }

    [Test, Performance]
    [Category("Performance")]
    public void UnsafeHashMap_Performance_SmallHashMap([Values(1000, 10000)] int iterations)
    {
        Measure.Method(() =>
        {
            for (var iter = 0; iter < iterations; ++iter)
            {
                // Intentionally setting capacity to 16 but adding 32 items.
                using (var container = new UnsafeHashMap<int, int>(16, Allocator.Persistent))
                {
                    for (int i = 0; i < 32; ++i)
                    {
                        container.Add(i, i);
                    }
                }
            }
        })
        .WarmupCount(10)
        .MeasurementCount(10)
        .Run();
    }

    [Test, Performance]
    [Category("Performance")]
    public void UnsafeHashMap_Performance_RepeatLookup([Values(10, 100, 1000, 10000, 100000)] int insertions)
    {
        using (var container = new UnsafeHashMap<int, int>(insertions, Allocator.Persistent))
        {
            using (var addedKeys = new NativeList<int>(insertions, Allocator.Persistent))
            {
                Random.InitState(0);
                for (int i = 0; i < insertions; i++)
                {
                    int randKey = Random.Range(0, insertions);
                    container.TryAdd(randKey, randKey);
                    addedKeys.Add(randKey);
                }

                Measure.Method(() =>
                    {
                        for (int i = 0; i < insertions; i++)
                        {
                            int randKey = addedKeys[i];
                            Assert.IsTrue(container.TryGetValue(randKey, out _));
                        }

                    })
                    .WarmupCount(10)
                    .MeasurementCount(10)
                    .Run();
            }
        }
    }

    [Test, Performance]
    [Category("Performance")]
    public void UnsafeHashMap_Performance_RepeatInsertAndLookup([Values(10, 100, 1000, 10000, 100000, 1000000, 2500000)] int insertions)
    {
        using (var container = new UnsafeHashMap<int, int>(insertions, Allocator.Persistent))
        {
            Random.InitState(0);
            Measure.Method(() =>
                {
                    for (int i = 0; i < insertions; i++)
                    {
                        int randKey = Random.Range(0, insertions);
                        container.TryAdd(randKey, randKey);
                        container.TryGetValue(randKey, out _);
                    }

                })
                .WarmupCount(10)
                .MeasurementCount(10)
                .Run();
        }
    }
}

#endif
