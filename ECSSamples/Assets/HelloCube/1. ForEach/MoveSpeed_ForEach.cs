using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct MoveSpeed_ForEach : IComponentData
{
    public Vector3 MoveSpeed;
}
