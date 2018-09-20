using System;
using Unity.Entities;

[Serializable]
public struct CubeComp : IComponentData
{
    public float Value;
}

[UnityEngine.DisallowMultipleComponent]
public class CubeComponent : ComponentDataWrapper<Radius>
{
}
