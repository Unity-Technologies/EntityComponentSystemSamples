using System;
using Unity.Entities;

// ReSharper disable once InconsistentNaming
[GenerateAuthoringComponent]
public struct RotationSpeed_ForEach : IComponentData
{
    public float RadiansPerSecond;
}
