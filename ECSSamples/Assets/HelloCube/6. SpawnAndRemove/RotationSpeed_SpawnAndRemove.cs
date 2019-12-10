using System;
using Unity.Entities;

// ReSharper disable once InconsistentNaming
[Serializable]
public struct RotationSpeed_SpawnAndRemove : IComponentData
{
    public float RadiansPerSecond;
}
