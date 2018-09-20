using System;
using Unity.Entities;

namespace Samples.Common
{
    [Serializable]
    public struct RotationSpeed : IComponentData
    {
        public float Value;
    }

    [UnityEngine.DisallowMultipleComponent]
    public class RotationSpeedComponent : ComponentDataWrapper<RotationSpeed> { } 
}
