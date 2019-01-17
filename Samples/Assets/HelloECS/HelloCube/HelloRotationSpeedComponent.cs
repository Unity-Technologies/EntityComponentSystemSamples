using System;
using Unity.Entities;

// Serializable attribute is for editor support.
[Serializable]
public struct HelloRotationSpeed : IComponentData
{
    public float Value;
}

// ComponentDataWrapper is for creating a Monobehaviour representation of this component (for editor support).
[UnityEngine.DisallowMultipleComponent]
public class HelloRotationSpeedComponent : ComponentDataWrapper<HelloRotationSpeed> { }
