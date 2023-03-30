// This code originates from https://github.cds.internal.unity3d.com/andy-bastable/SpatialTree
// Check that repo and ask for permission before using it in other projects

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace NativePriorityHeap
{
    public enum Comparison
    {
        Min,
        Max
    };
}

[NativeContainerSupportsDeallocateOnJobCompletion]
[NativeContainer]
public unsafe struct NativePriorityHeap<T> : IDisposable where T : unmanaged, IComparable<T>
{
    [NativeDisableUnsafePtrRestriction]
    T* m_Buffer;
    int m_Capacity;
    Allocator m_AllocatorLabel;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
    AtomicSafetyHandle m_Safety;
    [NativeSetClassTypeToNullOnSchedule] DisposeSentinel m_DisposeSentinel;
#endif

    int m_NumEntries;
    int m_CompareMultiplier;

    public NativePriorityHeap(int capacity, Allocator allocator, NativePriorityHeap.Comparison comparison = NativePriorityHeap.Comparison.Min)
    {
        long totalSize = UnsafeUtility.SizeOf<T>() * capacity;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        // Native allocation is only valid for Temp, Job and Persistent
        if (allocator <= Allocator.None)
            throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", "allocator");
        if (capacity < 0)
            throw new ArgumentOutOfRangeException("capacity", "Capacity must be >= 0");
#endif

        m_Buffer = (T*)UnsafeUtility.Malloc(totalSize, UnsafeUtility.AlignOf<T>(), allocator);

        m_Capacity = capacity;
        m_AllocatorLabel = allocator;
        m_NumEntries = 0;

        m_CompareMultiplier = (comparison == NativePriorityHeap.Comparison.Min) ? 1 : -1;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, allocator);
#endif
    }

    NativePriorityHeap(in NativeArray<T> array, int count, NativePriorityHeap.Comparison comparison = NativePriorityHeap.Comparison.Min)
    {
        m_Buffer = (T*)array.GetUnsafePtr<T>();

        m_Capacity = array.Length;
        m_AllocatorLabel = Allocator.None;
        m_NumEntries = count;

        m_CompareMultiplier = (comparison == NativePriorityHeap.Comparison.Min) ? 1 : -1;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, Allocator.Temp);
#endif
    }

    public static NativePriorityHeap<T> FromArray(in NativeArray<T> array, int count, NativePriorityHeap.Comparison comparison = NativePriorityHeap.Comparison.Min)
    {
        return new NativePriorityHeap<T>(array, count, comparison);
    }

    public bool IsCreated { get { return m_Capacity > 0; } }

    public void Dispose()
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

        UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);
        m_Buffer = null;
        m_Capacity = 0;
    }

    public void Clear()
    {
        m_NumEntries = 0;
    }

    public int Count { get { return m_NumEntries; } }
    public int Capacity { get { return m_Capacity; } }

    public void Push(T item)
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

        if (m_NumEntries >= m_Capacity)
            throw new InvalidOperationException(string.Format("Not enough capacity {0} for NativePriorityHeap of size {1}", m_Capacity, m_NumEntries));

        // add new entry to bottom
        *(m_Buffer + m_NumEntries) = item;

        BubbleUp(m_NumEntries);
        m_NumEntries++;
    }

    public NativeArray<T> AsArray()
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(m_Safety);
        var arraySafety = m_Safety;
        AtomicSafetyHandle.UseSecondaryVersion(ref arraySafety);
#endif
        var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(m_Buffer, m_Capacity, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, arraySafety);
#endif
        return array;
    }

    public T Peek()
    {
        if (m_NumEntries == 0)
            throw new InvalidOperationException("NativePriorityHeap is empty");

        T root = *m_Buffer;

        return root;
    }

    public T Pop()
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

        if (m_NumEntries == 0)
            throw new InvalidOperationException("NativePriorityHeap is empty");

        T root = *m_Buffer;

        // reduce count and swap last entry to top
        *m_Buffer = *(m_Buffer + m_NumEntries - 1);
        m_NumEntries--;

        // bubble down to ensure heap is ordered
        BubbleDown(0);

        return root;
    }

    void BubbleUp(int i)
    {
        T* entryPtr = m_Buffer + i;
        int parentIndex = GetParentIndex(i);

        while (entryPtr > m_Buffer
                && ((m_Buffer + parentIndex)->CompareTo(*entryPtr) * m_CompareMultiplier) > 0)
        {
            // swap
            T parentEntry = *(m_Buffer + parentIndex);
            *(m_Buffer + parentIndex) = *entryPtr;
            *entryPtr = parentEntry;

            entryPtr = m_Buffer + parentIndex;
            i = parentIndex;
            parentIndex = GetParentIndex(i);
        }
    }

    void BubbleDown(int initialIndex)
    {
        int smallestIndex = initialIndex;
        T* initialEntryPtr = m_Buffer + initialIndex;
        T* smallestEntryPtr = m_Buffer + smallestIndex;

        int leftIndex = GetLeftChildIndex(initialIndex);
        if (leftIndex < m_NumEntries)
        {
            T* leftEntryPtr = m_Buffer + leftIndex;
            if ((initialEntryPtr->CompareTo(*leftEntryPtr) * m_CompareMultiplier) > 0)
            {
                smallestIndex = leftIndex;
                smallestEntryPtr = leftEntryPtr;
            }
        }

        int rightIndex = GetRightChildIndex(initialIndex);
        if (rightIndex < m_NumEntries)
        {
            T* rightEntryPtr = m_Buffer + rightIndex;
            if ((smallestEntryPtr->CompareTo(*rightEntryPtr) * m_CompareMultiplier) > 0)
            {
                smallestIndex = rightIndex;
                smallestEntryPtr = rightEntryPtr;
            }
        }

        if (smallestIndex != initialIndex)
        {
            T temp = *initialEntryPtr;
            *initialEntryPtr = *smallestEntryPtr;
            *smallestEntryPtr = temp;

            BubbleDown(smallestIndex);
        }
    }

    int GetParentIndex(int i) { return (i - 1) / 2; }
    int GetLeftChildIndex(int i) { return 2 * i + 1; }
    int GetRightChildIndex(int i) { return 2 * i + 2; }
}