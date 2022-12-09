using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[Serializable]
[MaterialProperty("_PBRMatrix4")]
public struct OverrideMaterialMatrix4Data : IComponentData
{
    public float4x4 Value;
}

namespace Authoring
{
    [DisallowMultipleComponent]
    public class OverrideMaterialMatrix4 : MonoBehaviour
    {
        public Matrix4x4 matrix4;
    }

    public class OverrideMaterialMatrix4Baker : Baker<OverrideMaterialMatrix4>
    {
        public override void Bake(OverrideMaterialMatrix4 authoring)
        {
            var data = new OverrideMaterialMatrix4Data { Value = authoring.matrix4 };
            AddComponent(data);
        }
    }
}

