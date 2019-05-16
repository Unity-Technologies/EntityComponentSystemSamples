using System;
using Unity.Entities;

// ReSharper disable once InconsistentNaming
[Serializable]
public struct RotationSpeed_IJobChunk : IComponentData
{
    public float RadiansPerSecond;
}
