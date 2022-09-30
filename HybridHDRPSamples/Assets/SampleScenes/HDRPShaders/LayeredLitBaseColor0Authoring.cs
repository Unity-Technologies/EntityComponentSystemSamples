using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace SampleScenes.TestHDRPShaders
{
    [MaterialProperty("_BaseColor0")]
    public struct LayeredLitBaseColor0 : IComponentData
    {
        public float4 Value;
    }

    [DisallowMultipleComponent]
    public class LayeredLitBaseColor0Authoring : MonoBehaviour
    {
        [RegisterBinding(typeof(LayeredLitBaseColor0), "Value.x", true)]
        [RegisterBinding(typeof(LayeredLitBaseColor0), "Value.y", true)]
        [RegisterBinding(typeof(LayeredLitBaseColor0), "Value.z", true)]
        [RegisterBinding(typeof(LayeredLitBaseColor0), "Value.w", true)]
        public float4 Value;

        class LayeredLitBaseColor0Baker : Baker<LayeredLitBaseColor0Authoring>
        {
            public override void Bake(LayeredLitBaseColor0Authoring authoring)
            {
                LayeredLitBaseColor0 component = default(LayeredLitBaseColor0);
                component.Value = authoring.Value;
                AddComponent(component);
            }
        }
    }
}
