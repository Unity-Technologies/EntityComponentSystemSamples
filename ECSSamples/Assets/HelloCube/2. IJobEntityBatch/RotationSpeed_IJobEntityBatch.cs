using System;
using Unity.Entities;

// ReSharper disable once InconsistentNaming
[Serializable]
public struct RotationSpeed_IJobEntityBatch : IComponentData
{
    public float RadiansPerSecond;
}
