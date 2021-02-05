using System;
using Unity.Entities;

// ReSharper disable once InconsistentNaming
[Serializable]
public struct RotationSpeed_IJobChunkStructBased : IComponentData
{
    public float RadiansPerSecond;
}
