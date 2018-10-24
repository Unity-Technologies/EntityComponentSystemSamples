using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;
using UnityEngine.TestTools;

public class BurstSafetyTests
{
    [BurstCompile(CompileSynchronously = true)]
    struct ThrowExceptionJob : IJobParallelFor
    {
        public void Execute(int index)
        {
            throw new System.ArgumentException("Blah");
        }
    }
    
    [Test]
    [Ignore("JOE FIXES IN PROGRESS")]
    public void ThrowExceptionParallelForStress()
    {
        LogAssert.Expect(LogType.Exception, new Regex("ArgumentException: Blah"));

        var jobData = new ThrowExceptionJob();
        jobData.Schedule(100, 1).Complete();
    }
    
    [BurstCompile(CompileSynchronously = true)]
    struct WriteToReadOnlyArrayJob : IJob
    {
        [ReadOnly]
        public NativeArray<int> test;
        public void Execute()
        {
            test[0] = 5;
        }
    }
    
    [Test]
    [Ignore("JOE FIXES IN PROGRESS")]
    public void WriteToReadOnlyArray()
    {
        LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException"));

        var jobData = new WriteToReadOnlyArrayJob();
        jobData.test = new NativeArray<int>(1, Allocator.Persistent);

        jobData.Run();

        jobData.test.Dispose();
    }
    
    [BurstCompile(CompileSynchronously = true)]
    struct ParallelForIndexChecksJob : IJobParallelFor
    {
        public NativeArray<int> test;

        public void Execute(int index)
        {
            test[0] = 5;
        }
    }
    
    [Test]
    [Ignore("JOE FIXES IN PROGRESS")]
    public void ParallelForMinMaxChecks()
    {
        LogAssert.Expect(LogType.Exception, new Regex("IndexOutOfRangeException"));

        var jobData = new ParallelForIndexChecksJob();
        jobData.test = new NativeArray<int>(2, Allocator.Persistent);

        jobData.Schedule(100, 1).Complete();

        jobData.test.Dispose();
    }

    [BurstCompile(CompileSynchronously = true)]
    struct AccessNullNativeArrayJob : IJobParallelFor
    {
        public void Execute(int index)
        {
            var array = new NativeArray<float>();
            array[0] = 5;
        }
    }

    [Test]
    [Ignore("Crashes Unity - Important")]
    public void AccessNullNativeArray()
    {
        LogAssert.Expect(LogType.Exception, new Regex("NullReferenceException"));

        new AccessNullNativeArrayJob().Schedule(100, 1).Complete();
    }

    [BurstCompile(CompileSynchronously = true)]
    unsafe struct AccessNullUnsafePtrJob : IJob
    {
#pragma warning disable 649
        [NativeDisableUnsafePtrRestriction] float* myArray;
#pragma warning restore 649

        public void Execute()
        {
            myArray[0] = 5;
        }
    }
    
    [Test]
    [Ignore("Crashes Unity - No user is supposed to write code like this, so not very important")]
    public void AccessNullUnsafePtr()
    {
        LogAssert.Expect(LogType.Exception, new Regex("NullReferenceException"));

        new AccessNullUnsafePtrJob().Run();
    }
}