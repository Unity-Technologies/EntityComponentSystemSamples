using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[Serializable]
#if ENABLE_HYBRID_RENDERER_V2
[MaterialProperty("_Color", MaterialPropertyFormat.Float4)]
#endif
public struct OverrideMaterialColorData : IComponentData
{
    public float4 Value;
}

[DisallowMultipleComponent]
public class OverrideMaterialColor : MonoBehaviour
{
    public Color color;
}

[WorldSystemFilter(WorldSystemFilterFlags.HybridGameObjectConversion)]
[ConverterVersion("unity", 1)]
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
