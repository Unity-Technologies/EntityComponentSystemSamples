using System;
using Unity.Entities;

namespace Samples.Common
{
    [Serializable]
    public struct LocalRotationSpeed : IComponentData
    {
        public float Value;
    }

    public class LocalRotationSpeedComponent : ComponentDataWrapper<LocalRotationSpeed> { } 
}
