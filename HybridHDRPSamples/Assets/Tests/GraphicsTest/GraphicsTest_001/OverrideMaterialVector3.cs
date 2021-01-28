using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[Serializable]
[MaterialProperty("_PBRVector3", MaterialPropertyFormat.Float3)]
public struct OverrideMaterialVector3Data : IComponentData
{
    public float3 Value;
}

[DisallowMultipleComponent]

public class OverrideMaterialVector3 : MonoBehaviour
{
    public Vector3 vec3;
}

[WorldSystemFilter(WorldSystemFilterFlags.HybridGameObjectConversion)]
[ConverterVersion("unity", 1)]
public class OverrideMaterialVector3System : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((OverrideMaterialVector3 comp) =>
        {
            var entity = GetPrimaryEntity(comp);
            var data = new OverrideMaterialVector3Data { Value = comp.vec3 };
            DstEntityManager.AddComponentData(entity, data);
        });
    }
}
