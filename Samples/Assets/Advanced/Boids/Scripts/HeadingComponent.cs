using System;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct Heading : IComponentData
{
    public float3 Value;

    public Heading(float3 heading)
    {
        Value = heading;
    }
}

[UnityEngine.DisallowMultipleComponent]
public class HeadingComponent : ComponentDataWrapper<Heading> { }
