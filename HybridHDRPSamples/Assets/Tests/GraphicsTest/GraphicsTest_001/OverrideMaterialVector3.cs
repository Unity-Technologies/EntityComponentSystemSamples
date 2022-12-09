using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[Serializable]
[MaterialProperty("_PBRVector3")]
public struct OverrideMaterialVector3Data : IComponentData
{
    public float3 Value;
}

namespace Authoring
{
    [DisallowMultipleComponent]
    public class OverrideMaterialVector3 : MonoBehaviour
    {
        public Vector3 vec3;
    }

    public class OverrideMaterialVector3Baker : Baker<OverrideMaterialVector3>
    {
        public override void Bake(OverrideMaterialVector3 authoring)
        {
            var data = new OverrideMaterialVector3Data { Value = authoring.vec3 };
            AddComponent(data);
        }
    }
}

