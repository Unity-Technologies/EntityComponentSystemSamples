using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[Serializable]
[MaterialProperty("_PBRVector2")]
public struct OverrideMaterialVector2Data : IComponentData
{
    public float2 Value;
}

namespace Authoring
{
    [DisallowMultipleComponent]
    public class OverrideMaterialVector2 : MonoBehaviour
    {
        public Vector2 vec2;
    }

    public class OverrideMaterialVector2Baker : Baker<OverrideMaterialVector2>
    {
        public override void Bake(OverrideMaterialVector2 authoring)
        {
            var data = new OverrideMaterialVector2Data { Value = authoring.vec2 };
            AddComponent(data);
        }
    }
}

