using System;
using Unity.Entities;

// ReSharper disable once InconsistentNaming
[GenerateAuthoringComponent]
public struct RotationSpeed_ForEach_ISystem : IComponentData
{
    public float RadiansPerSecond;
}
