using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public static class AtomicCounterHelper
{
    public static int AtomicAdd(this NativeReference<int> nativeReference, int value)
    {
        unsafe
        {
            return Interlocked.Add(ref UnsafeUtility.AsRef<int>(nativeReference.GetUnsafePtr()), value) - value;
        }
    }
}