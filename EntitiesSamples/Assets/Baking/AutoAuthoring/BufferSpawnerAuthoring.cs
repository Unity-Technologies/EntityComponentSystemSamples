using System;
using AutoAuthoring;
using Unity.Entities;
using Unity.Mathematics;

namespace Baking.AutoAuthoring
{
    public class BufferSpawnerAuthoring : BufferAutoAuthoring<BufferSpawner>
    {
        // Defining OnEnable() makes the inspector show the enabled component checkbox.
        // Disabled components are not baked.
        void OnEnable() {}
    }

    [Serializable]
    public struct BufferSpawner : IBufferElementData
    {
        public Entity Prefab;
        public float3 Offset;
        public int InstanceCount;
    }
}
