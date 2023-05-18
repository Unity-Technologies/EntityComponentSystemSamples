# Unity.Collections cheat sheet

The collections provided by this package fall into three categories:

- The collection types in `Unity.Collections` whose names start with `Native-` have safety checks for ensuring that they're properly disposed and are used in a thread-safe manner. 
- The collection types in `Unity.Collections.LowLevel.Unsafe` whose names start with `Unsafe-` do not have these safety checks.
- The remaining collection types are not allocated and contain no pointers, so effectively their disposal and thread safety are never a concern. These types hold only small amounts of data.

## Allocators

- `Allocator.Temp`: The fastest allocator. For very short-lived allocations. Temp allocations *cannot* be passed into jobs.
- `Allocator.TempJob`: The next fastest allocator. For short-lived allocations (4-frame lifetime). TempJob allocations can be passed into jobs.
- `Allocator.Persistent`: The slowest allocator. For indefinite lifetime allocations. Persistent allocations can be passed into jobs.

## Array-like types

A few key array-like types are provided by the [core module](https://docs.unity3d.com/ScriptReference/UnityEngine.CoreModule), including [`Unity.Collections.NativeArray<T>`](https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeArray_1) and [`Unity.Collections.NativeSlice<T>`](https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeSlice_1). This package itself provides:

||| 
----------------------------------------------------- | -----------
[NativeList](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.NativeList-1.html)                       | A resizable list.
[UnsafeList](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.LowLevel.Unsafe.UnsafeList-1.html)       | A resizable list.
[UnsafePtrList](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.LowLevel.Unsafe.UnsafePtrList-1.html)    | A resizable list of pointers.
[NativeStream](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.NativeStream.html)                       | A set of append-only, untyped buffers.
[UnsafeStream](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.LowLevel.Unsafe.UnsafeStream.html)       | A set of append-only, untyped buffers.
[UnsafeAppendBuffer](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.LowLevel.Unsafe.UnsafeAppendBuffer.html) | An append-only untyped buffer.
[NativeQueue](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.NativeQueue-1.html)                      | A resizable queue.
[UnsafeRingQueue](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.LowLevel.Unsafe.UnsafeRingQueue-1.html)  | A fixed-size circular buffer.
[FixedList32Bytes](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.FixedList32Bytes-1.html)                 | A 32-byte list, including 2 bytes of overhead, so 30 bytes are available for storage. Max capacity depends upon the type parameter.
[FixedList64Bytes](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.FixedList64Bytes-1.html)                 | A 64-byte list, including 2 bytes of overhead, so 62 bytes are available for storage. Max capacity depends upon the type parameter.
[FixedList128Bytes](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.FixedList128Bytes-1.html)                 | A 128-byte list, including 2 bytes of overhead, so 126 bytes are available for storage. Max capacity depends upon the type parameter.
[FixedList512Bytes](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.FixedList512Bytes-1.html)                 | A 512-byte list, including 2 bytes of overhead, so 510 bytes are available for storage. Max capacity depends upon the type parameter.
[FixedList4096Bytes](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.FixedList4096Bytes-1.html)                 | A 4096-byte list, including 2 bytes of overhead, so 4094 bytes are available for storage. Max capacity depends upon the type parameter.

There are no multi-dimensional array types, but you can simply pack multi-dimensional data into a single-dimension: for example, for an `int[4][5]` array, use an `int[20]` array instead (because `4 * 5` is `20`).

When using the Entities package, a [DynamicBuffer](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Entities.DynamicBuffer-1.html) component is often the best choice for an array- or list-like collection.

See also [NativeArrayExtensions](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.NativeArrayExtensions.html), [ListExtensions](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.ListExtensions.html), [NativeSortExtension](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.NativeSortExtension.html).

## Map and set types

|||
---------------------------------------------------------------| -----------
[NativeParallelHashMap](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.NativeParallelHashMap-2.html)                      | An unordered associative array of key-value pairs.
[UnsafeParallelHashMap](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.LowLevel.Unsafe.UnsafeParallelHashMap-2.html)      | An unordered associative array of key-value pairs.
[NativeParallelHashSet](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.NativeParallelHashSet-1.html)                      | A set of unique values.
[UnsafeParallelHashSet](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.LowLevel.Unsafe.UnsafeParallelHashMap-2.html)      | A set of unique values.
[NativeMultiHashMap](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.NativeMultiHashMap-2.html)                 | An unordered associative array of key-value pairs. The keys do not have to be unique, *i.e.* two pairs can have equal keys.
[UnsafeMultiHashMap](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.LowLevel.Unsafe.UnsafeMultiHashMap-2.html) | An unordered associative array of key-value pairs. The keys do not have to be unique, *i.e.* two pairs can have equal keys.

See also [HashSetExtensions](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.HashSetExtensions.html), [Unity.Collections.NotBurstCompatible.Extensions](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.NotBurstCompatible.html), and [Unity.Collections.LowLevel.Unsafe.NotBurstCompatible.Extensions](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.LowLevel.Unsafe.NotBurstCompatible.Extensions.html)

## Bit arrays and bit fields

|||
------------------------------------------------- | -----------
[BitField32](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.BitField32.html)                     | A fixed-size array of 32 bits.
[BitField64](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.BitField64.html)                     | A fixed-size array of 64 bits.
[NativeBitArray](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.NativeBitArray.html)                 | An arbitrary-sized array of bits.
[UnsafeBitArray](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.LowLevel.Unsafe.UnsafeBitArray.html) | An arbitrary-sized array of bits.

## String types

|||
------------------------------------- | -----------
[NativeText](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.NativeText.html)         | A UTF-8 encoded string. Mutable and resizable.
[FixedString32Bytes](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.FixedString32Bytes.html) | A 32-byte UTF-8 encoded string, including 3 bytes of overhead, so 29 bytes available for storage.
[FixedString64Bytes](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.FixedString64Bytes.html) | A 64-byte UTF-8 encoded string, including 3 bytes of overhead, so 61 bytes available for storage.
[FixedString128Bytes](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.FixedString128Bytes.html) | A 128-byte UTF-8 encoded string, including 3 bytes of overhead, so 125 bytes available for storage.
[FixedString512Bytes](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.FixedString512Bytes.html) | A 512-byte UTF-8 encoded string, including 3 bytes of overhead, so 509 bytes available for storage.
[FixedString4096Bytes](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.FixedString4096Bytes.html) | A 4096-byte UTF-8 encoded string, including 3 bytes of overhead, so 4093 bytes available for storage.

See also [FixedString](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.FixedString.html) and [FixedStringMethods](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.FixedStringMethods.html).


## Other types

|||
-------------------------------------------------------- | -----------
[NativeReference](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.NativeReference-1.html)                     | A reference to a single value. Functionally equivalent to an array of length 1.
[UnsafeAtomicCounter32](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.LowLevel.Unsafe.UnsafeAtomicCounter32.html) | A 32-bit atomic counter.
[UnsafeAtomicCounter64](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/api/Unity.Collections.LowLevel.Unsafe.UnsafeAtomicCounter64.html) | A 64-bit atomic counter.


## Enumerators

Most of the collections have a `GetEnumerator` method, which returns an implementation of `IEnumerator<T>`. The enumerator's `MoveNext` method advances its `Current` property to the next element.

## Parallel readers and writers

Several of the collection types have nested types for reading and writing from parallel jobs. For example, to write safely to a `NativeList<T>` from a parallel job, you need a `NativeList<T>.ParallelWriter`.
