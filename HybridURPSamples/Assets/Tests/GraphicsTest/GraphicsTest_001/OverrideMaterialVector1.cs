using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[Serializable]
[MaterialProperty("_PBRVector1", MaterialPropertyFormat.Float)]
public struct OverrideMaterialVector1Data : IComponentData
{
    public float Value;
}

[DisallowMultipleComponent]
[ConverterVersion("unity", 1)]
public class OverrideMaterialVector1 : MonoBehaviour
{
    public float vec;
}

[WorldSystemFilter(WorldSystemFilterFlags.HybridGameObjectConversion)]
public class OverrideMaterialVector1System : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((OverrideMaterialVector1 comp) =>
        {
            var entity = GetPrimaryEntity(comp);
            var data = new OverrideMaterialVector1Data { Value = comp.vec };
            DstEntityManager.AddComponentData(entity, data);
        });
    }
}
