#if !UNITY_ENTITIES_0_12_OR_NEWER
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnsafeUtility_Collections = Unity.Collections.LowLevel.Unsafe.UnsafeUtility;

struct BufferTypeHandle<T> where T : struct, IBufferElementData
{
    public ArchetypeChunkBufferType<T> Value;
}

struct ComponentTypeHandle<T> where T : struct, IComponentData
{
    public ArchetypeChunkComponentType<T> Value;
}

static class ArchetypeChunkExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BufferAccessor<T> GetBufferAccessor<T>(this ArchetypeChunk chunk, BufferTypeHandle<T> bufferComponentType) where T : struct, IBufferElementData => chunk.GetBufferAccessor(bufferComponentType.Value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NativeArray<T> GetNativeArray<T>(this ArchetypeChunk chunk, ComponentTypeHandle<T> chunkComponentType) where T : struct, IComponentData => chunk.GetNativeArray(chunkComponentType.Value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Has<T>(this ArchetypeChunk chunk, BufferTypeHandle<T> chunkBufferType) where T : struct, IBufferElementData => chunk.Has(chunkBufferType.Value);
}

static class EntityCommandBufferExtensions
{
    public static EntityCommandBuffer.Concurrent AsParallelWriter(this EntityCommandBuffer commandBuffer) => commandBuffer.ToConcurrent();
}

static class UnsafeUtility_BackwardCompatibility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ref T AsRef<T>(void* ptr) where T : struct => ref UnsafeUtilityEx.AsRef<T>(ptr);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Free(void* memory, Allocator allocator) => UnsafeUtility_Collections.Free(memory, allocator);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* Malloc(long size, int alignment, Allocator allocator) => UnsafeUtility_Collections.Malloc(size, alignment, allocator);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void MemCpy(void* destination, void* source, long size) => UnsafeUtility_Collections.MemCpy(destination, source, size);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SizeOf<T>() where T : struct => UnsafeUtility_Collections.SizeOf<T>();
}
#endif
