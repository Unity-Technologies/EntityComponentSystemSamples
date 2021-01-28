using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[Serializable]
[MaterialProperty("_PBRVector2", MaterialPropertyFormat.Float2)]
public struct OverrideMaterialVector2Data : IComponentData
{
    public float2 Value;
}

[DisallowMultipleComponent]
public class OverrideMaterialVector2 : MonoBehaviour
{
    public Vector2 vec2;
}

[ConverterVersion("unity", 1)]
[WorldSystemFilter(WorldSystemFilterFlags.HybridGameObjectConversion)]
public class OverrideMaterialVector2System : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((OverrideMaterialVector2 comp) =>
        {
            var entity = GetPrimaryEntity(comp);
            var data = new OverrideMaterialVector2Data { Value = comp.vec2 };
            DstEntityManager.AddComponentData(entity, data);
        });
    }
}
