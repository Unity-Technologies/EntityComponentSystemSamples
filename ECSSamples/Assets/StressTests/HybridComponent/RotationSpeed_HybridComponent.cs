using System;
using Unity.Entities;

// Serializable attribute is for editor support.
// ReSharper disable once InconsistentNaming
[Serializable]
public struct RotationSpeed_HybridComponent : IComponentData
{
    public float RadiansPerSecond;
}
