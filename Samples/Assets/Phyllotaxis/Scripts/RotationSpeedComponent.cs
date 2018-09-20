using System;
using Unity.Entities;

[Serializable]
public struct RotationSpeed : IComponentData
{
    public float Value;
}

[UnityEngine.DisallowMultipleComponent]
public class RotationSpeedComponent : ComponentDataWrapper<RotationSpeed>
{
}
