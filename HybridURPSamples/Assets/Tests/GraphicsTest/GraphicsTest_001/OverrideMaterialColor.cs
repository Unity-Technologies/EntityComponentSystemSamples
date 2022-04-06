using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[Serializable]
[MaterialProperty("_Color", MaterialPropertyFormat.Float4)]
public struct OverrideMaterialColorData : IComponentData
{
    public float4 Value;
}

[DisallowMultipleComponent]
public class OverrideMaterialColor : MonoBehaviour
{
    public Color color;
}

[ConverterVersion("unity", 1)]
[WorldSystemFilter(WorldSystemFilterFlags.HybridGameObjectConversion)]
public class OverrideMaterialColorSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((OverrideMaterialColor comp) =>
        {
            var entity = GetPrimaryEntity(comp);
            var data = new OverrideMaterialColorData { Value = new float4(comp.color.r, comp.color.g, comp.color.b, comp.color.a) };
            DstEntityManager.AddComponentData(entity, data);
        });
    }
}
