using System;
using Unity.Entities;

[Serializable]
public struct Radius : IComponentData
{
    public float Value;
}

public class RadiusComponent : ComponentDataWrapper<Radius>
{
}
