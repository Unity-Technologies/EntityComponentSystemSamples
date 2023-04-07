using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace LearnSample
{
    [InternalBufferCapacity(8)]
    public struct PathPointComponent : IBufferElementData
    {
        public float3 Value;
    }

    public struct TargetPosComponent : IComponentData
    {
        public float3 Value;
    }

    public struct NextPathPointIndexComponent : IComponentData
    {
        public int Value;
    }
}
