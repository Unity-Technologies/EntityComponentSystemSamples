using System;
using Unity.Entities;

namespace Samples.Common
{
    /// <summary>
    /// This component specifies that if any other TransformPositionComponent is within the sphere defined by the
    /// TransformPositionComponent on this Entity and the specified radius, the TransformRotationComponent on that
    /// Entity should be set to speed, if it exists.
    /// </summary>
    [Serializable]
    public struct RotationSpeedResetSphere : IComponentData
    {
        public float speed;
    }

    public class RotationSpeedResetSphereComponent : ComponentDataWrapper<RotationSpeedResetSphere> { } 
}
