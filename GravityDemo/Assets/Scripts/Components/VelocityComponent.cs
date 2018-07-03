using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct Velocity : IComponentData
{
    public float3 Value;
}

class VelocityComponent : ComponentDataWrapper<Velocity>
{
}