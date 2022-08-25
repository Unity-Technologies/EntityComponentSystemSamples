using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections.Tests
{
    [BurstCompatible]
    unsafe struct RefReturn
    {
        private static readonly int* ptr = null;

        public ref int Property => ref UnsafeUtility.AsRef<int>(ptr);

        public static ref int StaticMethod()
        {
            return ref UnsafeUtility.AsRef<int>(ptr);
        }

        public ref int this[int i] => ref UnsafeUtility.AsRef<int>(ptr);

        public ref int Method() => ref UnsafeUtility.AsRef<int>(ptr);
    }

    [BurstCompatible]
    unsafe struct RefReadonlyReturn
    {
        private static readonly int* ptr = null;

        public ref readonly int Property => ref UnsafeUtility.AsRef<int>(ptr);

        public static ref readonly int StaticMethod()
        {
            return ref UnsafeUtility.AsRef<int>(ptr);
        }

        public ref readonly int this[int i] => ref UnsafeUtility.AsRef<int>(ptr);

        public ref readonly int Method() => ref UnsafeUtility.AsRef<int>(ptr);
    }

    [BurstCompatible]
    struct NotBurstCompatible
    {
        public int Compatible(int i)
        {
            return i + 12;
        }

        [NotBurstCompatible]
        public string NotCompatible(string str)
        {
            return str + "Not Burst Compatible!";
        }
    }

    [BurstCompatible]
    unsafe struct BurstCompatibleIndexerTest
    {
        double* ptr;

        public BurstCompatibleIndexerTest(double* p)
        {
            ptr = p;
        }

        public double this[int index]
        {
            get => ptr[index];
            set => ptr[index] = value;
        }
    }

    [BurstCompatible]
    unsafe struct BurstCompatibleMultiDimensionalIndexerTest
    {
        double* ptr;

        public BurstCompatibleMultiDimensionalIndexerTest(double* p)
        {
            ptr = p;
        }

        public double this[ulong index1, uint index2]
        {
            get => ptr[index1 + index2];
            set => ptr[index1 + index2] = value;
        }
    }

    // To verify this case https://unity3d.atlassian.net/browse/DOTS-3165
    [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
    struct BurstCompatibleUseSameGenericTypeWithDifferentStruct1<T> where T : struct
    {
        public T Value;

        public BurstCompatibleUseSameGenericTypeWithDifferentStruct1(T value)
        {
            Value = value;
        }

        public unsafe int CompareTo(BurstCompatibleUseSameGenericTypeWithDifferentStruct2<T> other)
        {
            return UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref other.Value), UnsafeUtility.AddressOf(ref Value), UnsafeUtility.SizeOf<T>());
        }
    }

    // To verify this case https://unity3d.atlassian.net/browse/DOTS-3165
    [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
    struct BurstCompatibleUseSameGenericTypeWithDifferentStruct2<T> where T : struct
    {
        public T Value;

        public BurstCompatibleUseSameGenericTypeWithDifferentStruct2(T value)
        {
            Value = value;
        }

        public unsafe int CompareTo(BurstCompatibleUseSameGenericTypeWithDifferentStruct1<T> other)
        {
            return UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref other.Value), UnsafeUtility.AddressOf(ref Value), UnsafeUtility.SizeOf<T>());
        }
    }

    [BurstCompatible]
    struct BurstCompatibleTypeWithPrivateMethods
    {
        private static int i = 0;

        // This method should not generate any errors or tests.
        int NotCompatible()
        {
            return ++i;
        }
    }

    [BurstCompile]
    [BurstCompatible]
    struct IgnoreBurstDirectCallPostfix
    {
        // This method is Burst direct callable (as of Burst 1.5.0). The IL post processor will generate a copy of this
        // method with a postfix attached to it, but the name will be an invalid identifier (ie - Foo$BurstManaged).
        // No test should be generated for this method or the copy generated during IL post processing, since
        // this method is private (Burst compatible skips private) and the copy contains an invalid character.
        [BurstCompile]
        private static int PrivateBurstDirectCall(int i)
        {
            return ++i;
        }

        // This method is Burst direct callable (as of Burst 1.5.0). One test should be generated for this
        // public method but no test for the copy which contains an invalid character.
        [BurstCompile]
        public static int PublicBurstDirectCall(int i)
        {
            return i + 2;
        }

        // This method is not Burst direct callable but a test should be generated for this public method.
        public static int CallPrivateBurstDirectCall(int i)
        {
            return PrivateBurstDirectCall(i);
        }
    }
}
