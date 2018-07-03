using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct Mass : IComponentData
{
    public float Value;
}

class MassComponent : ComponentDataWrapper<Mass>
{
}