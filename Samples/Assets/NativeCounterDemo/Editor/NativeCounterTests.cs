using Unity.Collections;
using Unity.Jobs;
using NUnit.Framework;

public class NativeCounterTests
{
    struct ParallelCountTo : IJobParallelFor
    {
        public NativeCounter.Concurrent counter;
        public int countMask;
        public void Execute(int i)
        {
            if ((i & countMask) == 0)
                counter.Increment();
        }
    }

	[Test]
    public void GetSetCount()
    {
        var counter = new NativeCounter(Allocator.Temp);
        Assert.AreEqual(0, counter.Count);
        counter.Count = 42;
        Assert.AreEqual(42, counter.Count);
        counter.Count = 3;
        Assert.AreEqual(3, counter.Count);
        counter.Dispose();
    }
	[Test]
    public void Increment()
    {
        var counter = new NativeCounter(Allocator.Temp);
        Assert.AreEqual(0, counter.Count);
        counter.Increment();
        Assert.AreEqual(1, counter.Count);
        counter.Increment();
        counter.Increment();
        Assert.AreEqual(3, counter.Count);
        counter.Dispose();
    }
	[Test]
    public void ConcurrentIncrement()
    {
        var counter = new NativeCounter(Allocator.Temp);
        NativeCounter.Concurrent concurrentCounter = counter;
        Assert.AreEqual(0, counter.Count);
        concurrentCounter.Increment();
        Assert.AreEqual(1, counter.Count);
        concurrentCounter.Increment();
        concurrentCounter.Increment();
        Assert.AreEqual(3, counter.Count);
        counter.Dispose();
    }
	[Test]
    public void SetCountIncrement()
    {
        var counter = new NativeCounter(Allocator.Temp);
        Assert.AreEqual(0, counter.Count);
        counter.Increment();
        Assert.AreEqual(1, counter.Count);
        counter.Count = 40;
        Assert.AreEqual(40, counter.Count);
        counter.Increment();
        counter.Increment();
        Assert.AreEqual(42, counter.Count);
        counter.Dispose();
    }
	[Test]
    public void SetCountConcurrentIncrement()
    {
        var counter = new NativeCounter(Allocator.Temp);
        NativeCounter.Concurrent concurrentCounter = counter;
        Assert.AreEqual(0, counter.Count);
        concurrentCounter.Increment();
        Assert.AreEqual(1, counter.Count);
        counter.Count = 40;
        Assert.AreEqual(40, counter.Count);
        concurrentCounter.Increment();
        concurrentCounter.Increment();
        Assert.AreEqual(42, counter.Count);
        counter.Dispose();
    }
	[Test]
    public void ParallelIncrement()
    {
        var counter = new NativeCounter(Allocator.Temp);
        var jobData = new ParallelCountTo();
        jobData.counter = counter;
        // Count every second item
        jobData.countMask = 1;
        jobData.Schedule(1000000, 1).Complete();
        Assert.AreEqual(500000, counter.Count);
        counter.Dispose();
    }
	[Test]
    public void ParallelIncrementSetCount()
    {
        var counter = new NativeCounter(Allocator.Temp);
        var jobData = new ParallelCountTo();
        jobData.counter = counter;
        counter.Count = 42;
        // Count every second item
        jobData.countMask = 1;
        jobData.Schedule(1000, 1).Complete();
        Assert.AreEqual(542, counter.Count);
        counter.Dispose();
    }
}

public class NativePerThreadCounterTests
{
    struct ParallelCountTo : IJobParallelFor
    {
        public NativePerThreadCounter.Concurrent counter;
        public int countMask;
        public void Execute(int i)
        {
            if ((i & countMask) == 0)
                counter.Increment();
        }
    }

	[Test]
    public void GetSetCount()
    {
        var counter = new NativePerThreadCounter(Allocator.Temp);
        Assert.AreEqual(0, counter.Count);
        counter.Count = 42;
        Assert.AreEqual(42, counter.Count);
        counter.Count = 3;
        Assert.AreEqual(3, counter.Count);
        counter.Dispose();
    }
	[Test]
    public void Increment()
    {
        var counter = new NativePerThreadCounter(Allocator.Temp);
        Assert.AreEqual(0, counter.Count);
        counter.Increment();
        Assert.AreEqual(1, counter.Count);
        counter.Increment();
        counter.Increment();
        Assert.AreEqual(3, counter.Count);
        counter.Dispose();
    }
	[Test]
    public void ConcurrentIncrement()
    {
        var counter = new NativePerThreadCounter(Allocator.Temp);
        NativePerThreadCounter.Concurrent concurrentCounter = counter;
        Assert.AreEqual(0, counter.Count);
        concurrentCounter.Increment();
        Assert.AreEqual(1, counter.Count);
        concurrentCounter.Increment();
        concurrentCounter.Increment();
        Assert.AreEqual(3, counter.Count);
        counter.Dispose();
    }
	[Test]
    public void SetCountIncrement()
    {
        var counter = new NativePerThreadCounter(Allocator.Temp);
        Assert.AreEqual(0, counter.Count);
        counter.Increment();
        Assert.AreEqual(1, counter.Count);
        counter.Count = 40;
        Assert.AreEqual(40, counter.Count);
        counter.Increment();
        counter.Increment();
        Assert.AreEqual(42, counter.Count);
        counter.Dispose();
    }
	[Test]
    public void SetCountConcurrentIncrement()
    {
        var counter = new NativePerThreadCounter(Allocator.Temp);
        NativePerThreadCounter.Concurrent concurrentCounter = counter;
        Assert.AreEqual(0, counter.Count);
        concurrentCounter.Increment();
        Assert.AreEqual(1, counter.Count);
        counter.Count = 40;
        Assert.AreEqual(40, counter.Count);
        concurrentCounter.Increment();
        concurrentCounter.Increment();
        Assert.AreEqual(42, counter.Count);
        counter.Dispose();
    }
	[Test]
    public void ParallelIncrement()
    {
        var counter = new NativePerThreadCounter(Allocator.Temp);
        var jobData = new ParallelCountTo();
        jobData.counter = counter;
        // Count every second item
        jobData.countMask = 1;
        jobData.Schedule(1000000, 1).Complete();
        Assert.AreEqual(500000, counter.Count);
        counter.Dispose();
    }
	[Test]
    public void ParallelIncrementSetCount()
    {
        var counter = new NativePerThreadCounter(Allocator.Temp);
        var jobData = new ParallelCountTo();
        jobData.counter = counter;
        counter.Count = 42;
        // Count every second item
        jobData.countMask = 1;
        jobData.Schedule(1000, 1).Complete();
        Assert.AreEqual(542, counter.Count);
        counter.Dispose();
    }
}
