using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_URPMyColor", MaterialPropertyFormat.Float4)]
    struct URPMyColorVector4Override : IComponentData
    {
        public float4 Value;
    }
}
