using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_HDRPUnlitMyColor", MaterialPropertyFormat.Float4)]
    struct HDRPUnlitMyColorVector4Override : IComponentData
    {
        public float4 Value;
    }
}
