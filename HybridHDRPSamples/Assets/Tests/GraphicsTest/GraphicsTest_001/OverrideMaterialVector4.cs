using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[Serializable]
[MaterialProperty("_PBRVector4")]
public struct OverrideMaterialVector4Data : IComponentData
{
    public float4 Value;
}

namespace Authoring
{
    [DisallowMultipleComponent]
    public class OverrideMaterialVector4 : MonoBehaviour
    {
        public Vector4 vec4;
    }

    public class OverrideMaterialVector4Baker : Baker<OverrideMaterialVector4>
    {
        public override void Bake(OverrideMaterialVector4 authoring)
        {
            var data = new OverrideMaterialVector4Data { Value = authoring.vec4 };
            AddComponent(data);
        }
    }
}