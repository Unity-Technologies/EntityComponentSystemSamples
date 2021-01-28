using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_HDRPMyColor", MaterialPropertyFormat.Float4)]
    struct HDRPMyColorVector4Override : IComponentData
    {
        public float4 Value;
    }
}
