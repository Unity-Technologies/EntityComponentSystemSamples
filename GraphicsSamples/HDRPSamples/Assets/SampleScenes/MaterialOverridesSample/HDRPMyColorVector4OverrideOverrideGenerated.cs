using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_HDRPMyColor")]
    struct HDRPMyColorVector4Override : IComponentData
    {
        public float4 Value;
    }
}
