using System;
using Unity.Entities;

namespace Samples.Common
{
    /// <summary>
    /// This component specifies that if any other PositionComponent is within the sphere defined by the
    /// PositionComponent on this Entity and the specified radius, the TransformRotationComponent on that
    /// Entity should be set to speed, if it exists.
    /// </summary>
    [Serializable]
    public struct RotationSpeedResetSphere : IComponentData
    {
        public float speed;
    }

    [UnityEngine.DisallowMultipleComponent]
    public class RotationSpeedResetSphereComponent : ComponentDataWrapper<RotationSpeedResetSphere> { } 
}
