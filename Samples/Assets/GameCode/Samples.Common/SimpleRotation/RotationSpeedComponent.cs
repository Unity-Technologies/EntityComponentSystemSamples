using System;
using Unity.Entities;

namespace Samples.Common
{
    [Serializable]
    public struct RotationSpeed : IComponentData
    {
        public float Value;
    }

    public class RotationSpeedComponent : ComponentDataWrapper<RotationSpeed> { } 
}
