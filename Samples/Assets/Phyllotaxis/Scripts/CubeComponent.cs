using System;
using Unity.Entities;

[Serializable]
public struct CubeComp : IComponentData
{
    public float Value;
}

public class CubeComponent : ComponentDataWrapper<Radius>
{
}
