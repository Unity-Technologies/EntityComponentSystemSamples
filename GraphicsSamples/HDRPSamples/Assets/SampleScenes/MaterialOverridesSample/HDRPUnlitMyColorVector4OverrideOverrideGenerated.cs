using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_HDRPUnlitMyColor")]
    struct HDRPUnlitMyColorVector4Override : IComponentData
    {
        public float4 Value;
    }
}
