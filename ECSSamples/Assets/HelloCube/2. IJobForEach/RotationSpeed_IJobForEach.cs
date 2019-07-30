using System;
using Unity.Entities;

// ReSharper disable once InconsistentNaming
[Serializable]
public struct RotationSpeed_IJobForEach : IComponentData
{
    public float RadiansPerSecond;
}
