using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

#if ENABLE_HYBRID_RENDERER_V2
namespace Scenes.TestDuplicateProperties
{
    [GenerateAuthoringComponent]
    [MaterialProperty("_Color", MaterialPropertyFormat.Float4)]
    public struct DuplicateTestColorA : IComponentData
    {
        public float4 Value;
    }
}
#endif
