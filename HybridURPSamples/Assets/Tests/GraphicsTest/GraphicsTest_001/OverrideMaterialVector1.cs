using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[Serializable]
[MaterialProperty("_PBRVector1")]
public struct OverrideMaterialVector1Data : IComponentData
{
    public float Value;
}

namespace Authoring
{
    [DisallowMultipleComponent]
    public class OverrideMaterialVector1 : MonoBehaviour
    {
        public float vec;
    }

    public class OverrideMaterialVector1Baker : Baker<OverrideMaterialVector1>
    {
        public override void Bake(OverrideMaterialVector1 authoring)
        {
            var data = new OverrideMaterialVector1Data { Value = authoring.vec };
            AddComponent(data);
        }
    }
}

