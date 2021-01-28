using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_URPUnlitMyColor", MaterialPropertyFormat.Float4)]
    struct URPUnlitMyColorVector4Override : IComponentData
    {
        public float4 Value;
    }
}
