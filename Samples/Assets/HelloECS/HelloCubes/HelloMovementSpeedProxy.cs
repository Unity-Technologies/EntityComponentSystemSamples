using System;
using Unity.Entities;

// Serializable attribute is for editor support.
[Serializable]
public struct HelloMovementSpeed : IComponentData
{
    public float Value;
}

// ComponentDataProxy is for creating a MonoBehaviour representation of this component (for editor support).
[UnityEngine.DisallowMultipleComponent]
public class HelloMovementSpeedProxy : ComponentDataProxy<HelloMovementSpeed> { }
