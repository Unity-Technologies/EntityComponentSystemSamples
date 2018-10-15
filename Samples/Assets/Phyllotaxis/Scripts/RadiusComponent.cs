using System;
using Unity.Entities;

[Serializable]
public struct Radius : IComponentData
{
    public float Value;
}

[UnityEngine.DisallowMultipleComponent]
public class RadiusComponent : ComponentDataWrapper<Radius>
{
}
