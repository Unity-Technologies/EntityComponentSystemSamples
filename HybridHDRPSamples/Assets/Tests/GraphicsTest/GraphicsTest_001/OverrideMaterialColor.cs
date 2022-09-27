using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[Serializable]
[MaterialProperty("_Color")]
public struct OverrideMaterialColorData : IComponentData
{
    public float4 Value;
}

namespace Authoring
{
    [DisallowMultipleComponent]
    public class OverrideMaterialColor : MonoBehaviour
    {
        public Color color;
    }

    public class OverrideMaterialColorBaker : Baker<OverrideMaterialColor>
    {
        public override void Bake(OverrideMaterialColor authoring)
        {
            Color linearCol = authoring.color.linear;
            var data = new OverrideMaterialColorData { Value = new float4(linearCol.r, linearCol.g, linearCol.b, linearCol.a) };
            AddComponent(data);
        }
    }
}