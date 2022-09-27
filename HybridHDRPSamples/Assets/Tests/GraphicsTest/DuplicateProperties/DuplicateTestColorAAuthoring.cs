using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace Scenes.TestDuplicateProperties
{
    [MaterialProperty("_DuplicateColor")]
    public struct DuplicateTestColorA : IComponentData
    {
        public float4 Value;
    }

    [DisallowMultipleComponent]
    public class DuplicateTestColorAAuthoring : MonoBehaviour
    {
        [RegisterBinding(typeof(DuplicateTestColorA), "Value.x", true)]
        [RegisterBinding(typeof(DuplicateTestColorA), "Value.y", true)]
        [RegisterBinding(typeof(DuplicateTestColorA), "Value.z", true)]
        [RegisterBinding(typeof(DuplicateTestColorA), "Value.w", true)]
        public float4 Value;

        class DuplicateTestColorABaker : Baker<DuplicateTestColorAAuthoring>
        {
            public override void Bake(DuplicateTestColorAAuthoring authoring)
            {
                DuplicateTestColorA component = default(DuplicateTestColorA);
                component.Value = authoring.Value;
                AddComponent(component);
            }
        }
    }
}
