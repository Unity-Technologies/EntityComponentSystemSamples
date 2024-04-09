using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace Graphical.ShaderGraph
{
    public class ShaderOverridesAuthoring : MonoBehaviour
    {
        [Range(0, 1)] public float CenterSize = 0.5f;
        public Color LeftColor = Color.red;
        public Color RightColor = Color.blue;

        class Baker : Baker<ShaderOverridesAuthoring>
        {
            public override void Bake(ShaderOverridesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ShaderOverrideCenterSize { Value = authoring.CenterSize });
                AddComponent(entity, new ShaderOverrideLeftColor { Value = (Vector4)authoring.LeftColor });
                AddComponent(entity, new ShaderOverrideRightColor { Value = (Vector4)authoring.RightColor });
            }
        }
    }

    [MaterialProperty("Vector1_d334671a210a44d3b58d89879b1dceae")]
    public struct ShaderOverrideCenterSize : IComponentData
    {
        public float Value;
    }

    [MaterialProperty("Color_d9b47626e873463fbd997c9a6a857bf2")]
    public struct ShaderOverrideLeftColor : IComponentData
    {
        public float4 Value;
    }

    [MaterialProperty("Color_5e06a8bc7fbe4284bef1dcf16b184948")]
    public struct ShaderOverrideRightColor : IComponentData
    {
        public float4 Value;
    }
}
