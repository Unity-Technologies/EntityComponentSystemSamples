using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[Serializable]
[MaterialProperty("_PBRMatrix4", MaterialPropertyFormat.Float4x4)]
public struct OverrideMaterialMatrix4Data : IComponentData
{
    public float4x4 Value;
}

[DisallowMultipleComponent]
public class OverrideMaterialMatrix4 : MonoBehaviour
{
    public Matrix4x4 matrix4;
}

[ConverterVersion("unity", 1)]
[WorldSystemFilter(WorldSystemFilterFlags.HybridGameObjectConversion)]
public class OverrideMaterialMatrix4System : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((OverrideMaterialMatrix4 comp) =>
        {
            var entity = GetPrimaryEntity(comp);
            var data = new OverrideMaterialMatrix4Data { Value = float4x4.zero };
            DstEntityManager.AddComponentData(entity, data);
        });
    }
}
