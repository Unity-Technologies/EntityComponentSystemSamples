using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Samples.Common
{
    /// <summary>
    /// This component will update the corresponding PositionComponent associated with this component at the
    /// rate specified by the MoveSpeedComponent, also associated with this component in radians per second.
    /// </summary>
    [Serializable]
    public struct MoveAlongCircle : IComponentData
    {
        public float3 center;
        public float radius;
        [NonSerialized]
        public float t;
    }

    [UnityEngine.DisallowMultipleComponent]
    public class MoveAlongCircleComponent : ComponentDataWrapper<MoveAlongCircle> { } 
}

