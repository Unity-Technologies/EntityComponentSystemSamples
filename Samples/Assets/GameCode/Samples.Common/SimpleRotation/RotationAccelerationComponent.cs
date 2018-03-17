using System;
using Unity.Entities;

namespace Samples.Common
{
    [Serializable]
    public struct RotationAcceleration : IComponentData
    {
        public float speed;
    }

    public class RotationAccelerationComponent : ComponentDataWrapper<RotationAcceleration> { } 
}
