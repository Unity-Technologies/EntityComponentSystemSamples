using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace Scenes.TestDuplicateProperties
{
    [MaterialProperty("_DuplicateColor")]
    public struct DuplicateTestColorB : IComponentData
    {
        public float4 Value;
    }

    [DisallowMultipleComponent]
    public class DuplicateTestColorBAuthoring : MonoBehaviour
    {
        [RegisterBinding(typeof(DuplicateTestColorB), "Value.x", true)]
        [RegisterBinding(typeof(DuplicateTestColorB), "Value.y", true)]
        [RegisterBinding(typeof(DuplicateTestColorB), "Value.z", true)]
        [RegisterBinding(typeof(DuplicateTestColorB), "Value.w", true)]
        public float4 Value;

        class DuplicateTestColorBBaker : Baker<DuplicateTestColorBAuthoring>
        {
            public override void Bake(DuplicateTestColorBAuthoring authoring)
            {
                DuplicateTestColorB component = default(DuplicateTestColorB);
                component.Value = authoring.Value;
                AddComponent(component);
            }
        }
    }
}
