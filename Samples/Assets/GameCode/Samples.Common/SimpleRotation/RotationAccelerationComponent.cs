using System;
using Unity.Entities;

namespace Samples.Common
{
    [Serializable]
    public struct RotationAcceleration : IComponentData
    {
        public float speed;
    }

    [UnityEngine.DisallowMultipleComponent]
    public class RotationAccelerationComponent : ComponentDataWrapper<RotationAcceleration> { } 
}
