using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

// To be replaced by official versions in Unity.Collections when available.
public unsafe struct UnsafeRingQueue<T> : IDisposable
    where T : unmanaged
{
    [NativeDisableUnsafePtrRestriction]
    T* m_Buffer;
    Allocator m_Allocator;
    int m_Capacity;
    int m_StartIndex;
    int m_EndIndex;

    public int Count
    {
        get
        {
            var dist = m_EndIndex - m_StartIndex;
            return (dist < 0) ? ((m_Capacity - 1) + dist) : dist;
        }
    }

    public UnsafeRingQueue(int capacity, Allocator allocator)
    {
        m_Buffer = (T*)UnsafeUtility.Malloc(capacity * UnsafeUtility.SizeOf<T>(), 16, allocator);
        m_Allocator = allocator;
        m_StartIndex = 0;
        m_EndIndex = 0;
        m_Capacity = capacity;
    }

    public void Enqueue(T value)
    {
        m_Buffer[m_EndIndex] = value;
        m_EndIndex = (m_EndIndex+1) % m_Capacity;
    }

    public T Dequeue()
    {
        T value = m_Buffer[m_StartIndex];
        m_StartIndex = (m_StartIndex+1) % m_Capacity;
        return value;
    }
    
    void Deallocate()
    {
        if (m_Buffer != null)
            UnsafeUtility.Free(m_Buffer, m_Allocator);
    }
    
    public void Dispose()
    {
        Deallocate();
        m_Buffer = null;
    }
    
    public JobHandle Dispose(JobHandle inputDeps)
    {
        var jobHandle = new DisposeJob { Container = this }.Schedule(inputDeps);
        m_Buffer = null;

        return jobHandle;
    }

    
    [BurstCompile]
    struct DisposeJob : IJob
    {
        public UnsafeRingQueue<T> Container;

        public void Execute()
        {
            Container.Deallocate();
        }
    }
}
