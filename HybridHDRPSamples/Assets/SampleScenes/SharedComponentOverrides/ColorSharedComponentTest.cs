using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

public class ColorSharedComponentTest : MonoBehaviour
{
    public Color color;
}

[Serializable]
[MaterialProperty("_BaseColor", MaterialPropertyFormat.Float4)]
public struct ColorSharedComponent : IHybridSharedComponentFloat4Override, IEquatable<ColorSharedComponent>
{
    public float4 Value;

    public bool Equals(ColorSharedComponent other)
    {
        return Value.Equals(other.Value);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public float4 GetFloat4OverrideData()
    {
        return Value;
    }
}

[ConverterVersion("unity", 4)]
[WorldSystemFilter(WorldSystemFilterFlags.HybridGameObjectConversion)]
public class ColorSharedComponentConversion : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities
            .ForEach((ColorSharedComponentTest c) =>
            {
                var e = GetPrimaryEntity(c);
                var color = new float4(c.color.r, c.color.g, c.color.b, c.color.a);
                DstEntityManager.AddSharedComponentData(e, new ColorSharedComponent {Value = color});
            });
    }
}
