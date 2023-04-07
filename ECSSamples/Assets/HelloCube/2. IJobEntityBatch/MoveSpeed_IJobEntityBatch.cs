using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct MoveSpeed_IJobEntityBatch : IComponentData
{
    public Vector3 MoveSpeed;
}
