using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Burst;
using NUnit.Framework;

public class BurstTests
{
	[BurstCompile(CompileSynchronously = true)]
	public struct SimpleArrayAssignJob : IJob
	{
		public float value;
		public NativeArray<float> input;
		public NativeArray<float> output;

		public void Execute()
		{
			for (int i = 0; i != output.Length; i++)
				output[i] = i + value + input[i];
		}
	}

	[Test]
    public void SimpleFloatArrayAssignSimple()
    {
        var job = new SimpleArrayAssignJob();
        job.value = 10.0F;
        job.input = new NativeArray<float>(3, Allocator.Persistent);
        job.output = new NativeArray<float>(3, Allocator.Persistent);

        for (int i = 0;i != job.input.Length;i++)
            job.input[i] = 1000.0F * i;

        job.Schedule().Complete();

        Assert.AreEqual(3, job.output.Length);
        for (int i = 0;i != job.output.Length;i++)
            Assert.AreEqual(i + job.value + job.input[i], job.output[i]);

        job.input.Dispose();
        job.output.Dispose();
    }

	[BurstCompile(CompileSynchronously = true)]
	public struct SimpleArrayAssignForJob : IJobParallelFor
	{
		public float value;
		public NativeArray<float> input;
		public NativeArray<float> output;

		public void Execute(int i)
		{
			output[i] = i + value + input[i];
		}
	}
    [Test]
    public void SimpleFloatArrayAssignForEach()
    {
        var job = new SimpleArrayAssignForJob();
        job.value = 10.0F;
        job.input = new NativeArray<float>(1000, Allocator.Persistent);
        job.output = new NativeArray<float>(1000, Allocator.Persistent);

        for (int i = 0;i != job.input.Length;i++)
            job.input[i] = 1000.0F * i;

        job.Schedule(job.input.Length, 40).Complete();

        Assert.AreEqual(1000, job.output.Length);
        for (int i = 0; i != job.output.Length; i++)
            Assert.AreEqual(i + job.value + job.input[i], job.output[i]);

        job.input.Dispose();
        job.output.Dispose();
    }


	[BurstCompile(CompileSynchronously = true)]
	struct MallocTestJob : IJob
	{
		unsafe public void Execute()
		{
			void* allocated = UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>() * 100, 4, Allocator.Persistent);
			UnsafeUtility.Free(allocated, Allocator.Persistent);
		}

	}

	[Test]
	public void MallocTest()
	{
		var jobData = new MallocTestJob();
		jobData.Run();
	}


	[BurstCompile(CompileSynchronously = true)]
	struct ListCapacityJob : IJob
	{
        public NativeList<int> list;
		public void Execute()
		{
            list.Capacity = 100;
		}

	}

	[Test]
	public void ListCapacityJobTest()
	{
		var jobData = new ListCapacityJob() { list = new NativeList<int>(Allocator.TempJob) };
		jobData.Run();

		Assert.AreEqual(100, jobData.list.Capacity);
		Assert.AreEqual(0, jobData.list.Length);

        jobData.list.Dispose();
	}


	[BurstCompile(CompileSynchronously = true)]
	struct NativeListAssignValue : IJob
	{
		public NativeList<int> list;

		public void Execute()
		{
			list[0] = 1;
		}
	}
	[Test]
	public void AssignValue()
	{
		var jobData = new NativeListAssignValue() { list = new NativeList<int>(Allocator.TempJob) };
		jobData.list.Add(5);

		jobData.Run();

		Assert.AreEqual(1, jobData.list.Length);
		Assert.AreEqual(1, jobData.list[0]);

		jobData.list.Dispose();
	}

	[BurstCompile(CompileSynchronously = true)]
	struct NativeListAddValue : IJob
	{
		public NativeList<int> list;

		public void Execute()
		{
			list.Add(1);
			list.Add(2);
		}
	}
	[Test]
	public void AddValue()
	{
		var jobData = new NativeListAddValue() { list = new NativeList<int>(1, Allocator.Persistent) };

		Assert.AreEqual(1, jobData.list.Capacity);
		jobData.list.Add(-1);

		jobData.Run();

		Assert.AreEqual(3, jobData.list.Length);
		Assert.AreEqual(-1, jobData.list[0]);
		Assert.AreEqual(1, jobData.list[1]);
		Assert.AreEqual(2, jobData.list[2]);

		jobData.list.Dispose();
	}

    unsafe struct ConditionalTestStruct
    {
	    public void* a;
	    public void* b;
    }

    [BurstCompile(CompileSynchronously = true)]
	unsafe struct PointerConditional : IJob
	{
	    [NativeDisableUnsafePtrRestriction]
	    public ConditionalTestStruct* t;

		public void Execute()
		{
		    t->b = t->a != null ? t->a : null;
		}
	}

	[Test]
	public unsafe void PointerConditionalDoesntCrash()
	{
	    ConditionalTestStruct dummy;
        dummy.a = (void*) 0x1122334455667788;
        dummy.b = null;
        var jobData = new PointerConditional { t = &dummy };
        jobData.Schedule().Complete();
        Assert.AreEqual((IntPtr) dummy.a, (IntPtr) dummy.b);
	}

}
