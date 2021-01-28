using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[Serializable]
[MaterialProperty("_PBRVector4", MaterialPropertyFormat.Float4)]
public struct OverrideMaterialVector4Data : IComponentData
{
    public float4 Value;
}

[DisallowMultipleComponent]

public class OverrideMaterialVector4 : MonoBehaviour
{
    public Vector4 vec4;
}

[WorldSystemFilter(WorldSystemFilterFlags.HybridGameObjectConversion)]
[ConverterVersion("unity", 1)]
public class OverrideMaterialVector4System : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((OverrideMaterialVector4 comp) =>
        {
            var entity = GetPrimaryEntity(comp);
            var data = new OverrideMaterialVector4Data { Value = comp.vec4 };
            DstEntityManager.AddComponentData(entity, data);
        });
    }
}
