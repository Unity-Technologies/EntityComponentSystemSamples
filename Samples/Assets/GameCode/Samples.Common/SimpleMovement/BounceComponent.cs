using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Samples.Common
{
    [Serializable]
    public struct Bounce : IComponentData
    {
        [NonSerialized] public float t;
        public float speed;
        public float3 height;
    }

    [UnityEngine.DisallowMultipleComponent]
    public class BounceComponent : ComponentDataWrapper<Bounce> { } 
}
