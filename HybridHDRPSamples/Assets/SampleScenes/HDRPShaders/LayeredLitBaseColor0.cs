using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace SampleScenes.TestHDRPShaders
{
    [GenerateAuthoringComponent]
    [MaterialProperty("_BaseColor0", MaterialPropertyFormat.Float4)]
    public struct LayeredLitBaseColor0 : IComponentData
    {
        public float4 Value;
    }
}
